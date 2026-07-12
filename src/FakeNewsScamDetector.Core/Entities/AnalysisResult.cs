using FakeNewsScamDetector.Core.Enums;

namespace FakeNewsScamDetector.Core.Entities;

public class AnalysisResult
{
    public int Id { get; set; }
    public string InputText { get; set; } = string.Empty;
    public string? InputUrl { get; set; }
    public VerdictType Verdict { get; set; }
    public double ConfidenceScore { get; set; }
    public double MlScore { get; set; }
    public double RuleScore { get; set; }
    public double UrlRiskScore { get; set; }
    public List<string> Reasons { get; set; } = new();
    public List<FactCheckFinding> FactCheckFindings { get; set; } = new();

    /// <summary>
    /// "role: content" lines from an AI conversational verification session,
    /// when this result came from the chat flow rather than the ML pipeline.
    /// Stored via a JSON converter (not the pipe-joined Reasons pattern)
    /// because free-form chat text can contain literal '|' characters.
    /// </summary>
    public List<string> ConversationTranscript { get; set; } = new();

    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
}
