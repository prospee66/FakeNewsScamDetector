using System.Text.RegularExpressions;
using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Services.AI;
using Microsoft.AspNetCore.Mvc;

namespace FakeNewsScamDetector.Web.Controllers;

public class ChatController : Controller
{
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

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] List<ChatMessage> conversation, CancellationToken cancellationToken)
    {
        if (conversation is null || conversation.Count == 0)
            return BadRequest("Conversation must contain at least one message.");

        var reply = await _verifier.AskAsync(conversation, cancellationToken);

        var verdict = TryExtractVerdict(reply);
        var saved = false;
        if (verdict is not null)
        {
            await SaveConversationAsync(conversation, reply, verdict.Value);
            saved = true;
        }

        return Json(new { reply, conversationSaved = saved });
    }

    private static VerdictType? TryExtractVerdict(string reply)
    {
        var match = VerdictPattern.Match(reply);
        if (!match.Success)
            return null;

        return Enum.TryParse<VerdictType>(match.Groups[1].Value, ignoreCase: true, out var verdict)
            ? verdict
            : null;
    }

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
            // A conversational verdict isn't produced by the scored ML/rule
            // pipeline, so there's no comparable confidence number to report —
            // 0.5 is a neutral placeholder, not a measured probability.
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
