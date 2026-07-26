namespace FakeNewsScamDetector.Core.Entities;

// One fact-checker's review of a claim, shown as-is so the user can judge
// the source and rating themselves.
public class FactCheckFinding
{
    // The claim as worded by the publisher (may not exactly match the
    // user's input text - it's whatever Google's Fact Check API matched).
    public string ClaimText { get; set; } = string.Empty;

    // Who published the review, e.g. "Snopes" or "PolitiFact".
    public string Publisher { get; set; } = string.Empty;

    // The publisher's own rating label, e.g. "False", "Pants on Fire",
    // "Mixture". Not normalized to our VerdictType on purpose - see
    // VerdictAggregator for why.
    public string TextualRating { get; set; } = string.Empty;

    // Link to the full review article, if the API returned one.
    public string ReviewUrl { get; set; } = string.Empty;
}
