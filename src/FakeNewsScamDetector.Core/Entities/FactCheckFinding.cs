namespace FakeNewsScamDetector.Core.Entities;

/// <summary>
/// One professional fact-checker's review of a claim, surfaced verbatim so
/// the user can judge the source and rating themselves rather than trusting
/// an automated verdict on a truth question.
/// </summary>
public class FactCheckFinding
{
    public string ClaimText { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string TextualRating { get; set; } = string.Empty;
    public string ReviewUrl { get; set; } = string.Empty;
}
