using System.Text.RegularExpressions;
using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Services.AI;
using Microsoft.AspNetCore.Mvc;

namespace FakeNewsScamDetector.Web.Controllers;

// The "AI Chat" feature: a free-form conversation with an LLM instead of
// the scored ML/rule pipeline (see AnalysisController for that one). The
// whole conversation is stateless from the server's point of view - the
// browser resends the full message history on every Send call (see
// Chat/Index.cshtml's JavaScript), and nothing is saved to the database
// until the assistant reaches an explicit verdict.
public class ChatController : Controller
{
    // Matches the exact "VERDICT: <value>" line the system prompt
    // (VerificationSystemPrompt) instructs the assistant to end with, once
    // it's ready to conclude. Multiline so it can appear anywhere in a
    // longer reply, but anchored to a whole line (^...$) so it can't
    // accidentally match the word "verdict" used in passing conversation.
    private static readonly Regex VerdictPattern = new(
        @"^VERDICT:\s*(Legitimate|Suspicious|Scam|FakeNews)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private readonly IConversationalVerifierService _verifier;
    private readonly IAnalysisRepository _repository;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IConversationalVerifierService verifier, IAnalysisRepository repository, ILogger<ChatController> logger)
    {
        _verifier = verifier;
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // POST /Chat/Send - called by fetch() from the chat page's JavaScript,
    // not a normal form post, hence [FromBody] JSON instead of a view
    // model. Returns JSON too: { reply, conversationSaved }.
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] List<ChatMessage> conversation, CancellationToken cancellationToken)
    {
        if (conversation is null || conversation.Count == 0)
            return BadRequest("Conversation must contain at least one message.");

        var reply = await _verifier.AskAsync(conversation, cancellationToken);

        // Only save to history once the assistant has actually reached a
        // conclusion - a mid-conversation clarifying question (e.g. "who
        // sent you this?") has no verdict line and correctly saves nothing.
        var verdict = TryExtractVerdict(reply);
        var saved = false;
        if (verdict is not null)
        {
            await SaveConversationAsync(conversation, reply, verdict.Value);
            saved = true;
        }

        return Json(new { reply, conversationSaved = saved });
    }

    // Returns null if the reply doesn't end with a "VERDICT: ..." line, or
    // if it does but the value somehow isn't one of the four known verdicts
    // (shouldn't happen given the system prompt, but Enum.TryParse fails
    // safely either way instead of throwing).
    private static VerdictType? TryExtractVerdict(string reply)
    {
        var match = VerdictPattern.Match(reply);
        if (!match.Success)
            return null;

        return Enum.TryParse<VerdictType>(match.Groups[1].Value, ignoreCase: true, out var verdict)
            ? verdict
            : null;
    }

    // Turns a finished chat conversation into an AnalysisResult so it shows
    // up alongside scored-pipeline scans in History/Dashboard.
    private async Task SaveConversationAsync(List<ChatMessage> conversation, string finalReply, VerdictType verdict)
    {
        var firstUserMessage = conversation.FirstOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))?.Content
            ?? conversation[0].Content;

        var transcript = conversation
            .Select(m => $"{m.Role}: {m.Content}")
            .Append($"assistant: {finalReply}")
            .ToList();

        var result = new AnalysisResult
        {
            InputText = firstUserMessage,
            Verdict = verdict,
            // no scored pipeline ran here, so there's nothing real to put in
            // ConfidenceScore - 0.5 is just a placeholder, not a measurement
            ConfidenceScore = 0.5,
            MlScore = 0,
            RuleScore = 0,
            UrlRiskScore = 0,
            Reasons = ["Verdict reached via AI conversation, not automated scoring — see the conversation transcript."],
            ConversationTranscript = transcript,
            AnalyzedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(result);
        _logger.LogInformation("Saved AI conversation verdict {Verdict} to analysis history.", verdict);
    }
}
