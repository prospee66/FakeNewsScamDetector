using FakeNewsScamDetector.Core.Enums;

namespace FakeNewsScamDetector.Core.Entities;

// The saved outcome of one scan - either the scored ML/rule/URL pipeline
// (VerdictAggregator) or an AI chat conversation (ChatController). Both
// paths write to this same table, which is why some fields only make sense
// for one path or the other (see the comments below).
public class AnalysisResult
{
    // Database primary key, assigned by EF Core on insert.
    public int Id { get; set; }

    // The raw text the user submitted (or the first user message, if this
    // came from the AI chat flow).
    public string InputText { get; set; } = string.Empty;

    // The first URL found inside InputText, if any. Null if the input was
    // plain text with no link.
    public string? InputUrl { get; set; }

    // The final classification shown to the user.
    public VerdictType Verdict { get; set; }

    // 0-1 overall confidence behind the verdict. For the scored pipeline
    // this is the weighted blend of MlScore/RuleScore/UrlRiskScore; for the
    // chat flow it's just a fixed placeholder (see ChatController) since
    // there's no equivalent scored breakdown.
    public double ConfidenceScore { get; set; }

    // 0-1 score from the ML.NET text classifier. 0 for chat-flow results.
    public double MlScore { get; set; }

    // 0-1 score from the keyword-based ScamRuleEngine. 0 for chat-flow results.
    public double RuleScore { get; set; }

    // 0-1 score from UrlAnalyzerService (domain age, Safe Browsing, etc.).
    // 0 if there was no URL to check, or if this came from the chat flow.
    public double UrlRiskScore { get; set; }

    // Human-readable explanations behind the verdict (e.g. "Mentions wire
    // transfer, a common scam payment method"). Empty for chat-flow results,
    // which instead use ConversationTranscript to show the reasoning.
    public List<string> Reasons { get; set; } = new();

    // Any matching claims found via the Google Fact Check API. Just
    // informational - not folded into the score (see VerdictAggregator).
    public List<FactCheckFinding> FactCheckFindings { get; set; } = new();

    // Only set when this result came from the AI chat flow instead of the
    // ML pipeline. Stored as JSON, not pipe-joined like Reasons, since chat
    // text can contain a literal "|".
    public List<string> ConversationTranscript { get; set; } = new();

    // When the scan was run, in UTC (not local time), so it sorts and
    // displays consistently regardless of server time zone.
    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
}
