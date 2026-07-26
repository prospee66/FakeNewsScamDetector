namespace FakeNewsScamDetector.Core.Entities;

// A seeded reference table of known scam keywords/phrases (see
// ScamPatternSeed) describing what a rule-based detector could look for.
// Note: the actual live rule engine (ScamRuleEngine) currently keeps its
// own hardcoded pattern list in code rather than reading from this table -
// this entity exists for future use if that ever moves into the database.
public class ScamPattern
{
    public int Id { get; set; }

    // The keyword or phrase to match against, e.g. "wire transfer".
    public string Pattern { get; set; } = string.Empty;

    // Human-readable explanation of why this pattern is a red flag.
    public string Description { get; set; } = string.Empty;

    // How much this pattern should contribute to a scam score if matched.
    public double WeightScore { get; set; }
}
