namespace FakeNewsScamDetector.Core.Entities;

// One fact-checker's review of a claim, shown as-is so the user can judge
// the source and rating themselves.
public class FactCheckFinding
{
    public string ClaimText { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string TextualRating { get; set; } = string.Empty;
    public string ReviewUrl { get; set; } = string.Empty;
}
