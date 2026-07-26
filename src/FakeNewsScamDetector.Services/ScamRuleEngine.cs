using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services;

// Simple, fast, and fully explainable scam detection: a hardcoded list of
// keyword/phrase red flags, each with a weight and a human-readable reason.
// Runs alongside the ML classifier in VerdictAggregator - deterministic and
// instant, so it's a good complement to a model that can be wrong or slow.
public class ScamRuleEngine : IScamRuleEngine
{
    // (keyword to match, how much it adds to the score, why it's a red flag)
    private static readonly (string Keyword, double Weight, string Reason)[] Rules =
    [
        ("wire transfer", 0.25, "Mentions wire transfer, a common scam payment method"),
        ("gift card", 0.25, "Requests payment via gift card, a classic scam red flag"),
        ("urgent", 0.10, "Uses urgency to pressure quick action"),
        ("act now", 0.15, "Uses urgency to pressure quick action"),
        ("verify your account", 0.20, "Phishing-style account verification request"),
        ("suspended", 0.15, "Threatens account suspension"),
        ("lottery", 0.30, "References winning a lottery or prize you didn't enter"),
        ("inheritance", 0.30, "Unsolicited inheritance claim, a common advance-fee scam"),
        ("social security number", 0.25, "Requests sensitive personal identification data"),
        ("bitcoin", 0.15, "Requests payment in cryptocurrency"),
        ("click here immediately", 0.20, "Pressures immediate click-through"),
        ("congratulations you have been selected", 0.25, "Unsolicited prize/selection notification"),
    ];

    public (double Score, List<string> MatchedReasons) EvaluateText(string text)
    {
        var lowered = text.ToLowerInvariant();
        var reasons = new List<string>();
        double score = 0;

        // Every matching rule adds its weight - a message that trips
        // multiple red flags (e.g. "urgent" AND "gift card") scores higher
        // than one that only trips a single, milder rule.
        foreach (var (keyword, weight, reason) in Rules)
        {
            if (lowered.Contains(keyword))
            {
                score += weight;
                reasons.Add(reason);
            }
        }

        // Weights can add up past 1.0 if several rules match at once; cap
        // it so the result stays a valid 0-1 score like the ML scores it's
        // blended with in VerdictAggregator.
        return (Math.Min(score, 1.0), reasons);
    }
}
