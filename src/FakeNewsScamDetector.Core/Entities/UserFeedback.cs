namespace FakeNewsScamDetector.Core.Entities;

public class UserFeedback
{
    public int Id { get; set; }
    public int AnalysisResultId { get; set; }
    public bool WasAccurate { get; set; }
    public string? Comment { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
