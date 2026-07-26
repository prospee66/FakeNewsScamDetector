namespace FakeNewsScamDetector.Core.Entities;

// Meant to hold a user's "was this verdict right?" feedback on a past
// AnalysisResult. The database table exists (see AppDbContext), but no
// controller or view currently writes to it - there's no feedback form in
// the UI yet. Left in place for when that feature gets built.
public class UserFeedback
{
    public int Id { get; set; }

    // Which AnalysisResult this feedback is about.
    public int AnalysisResultId { get; set; }

    // Whether the user thought the verdict was correct.
    public bool WasAccurate { get; set; }

    // Optional free-text explanation from the user.
    public string? Comment { get; set; }

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
