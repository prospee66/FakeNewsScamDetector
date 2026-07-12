using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services;

public class ScamRuleEngine : IScamRuleEngine
{
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

        foreach (var (keyword, weight, reason) in Rules)
        {
            if (lowered.Contains(keyword))
            {
                score += weight;
                reasons.Add(reason);
            }
        }

        return (Math.Min(score, 1.0), reasons);
    }
}
