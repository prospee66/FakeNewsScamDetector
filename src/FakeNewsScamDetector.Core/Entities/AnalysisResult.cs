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

    // Only set when this result came from the AI chat flow instead of the
    // ML pipeline. Stored as JSON, not pipe-joined like Reasons, since chat
    // text can contain a literal "|".
    public List<string> ConversationTranscript { get; set; } = new();

    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
}
