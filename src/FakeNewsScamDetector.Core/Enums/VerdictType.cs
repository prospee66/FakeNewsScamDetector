namespace FakeNewsScamDetector.Core.Enums;

// The four possible outcomes a scan can end in. Order matters for the
// "worse than" comparisons implied throughout the UI (badge colors, etc.):
// Legitimate < Suspicious < (Scam or FakeNews).
public enum VerdictType
{
    // No red flags found, or too few to be concerning.
    Legitimate,

    // Some red flags found, but not enough to confidently call it a scam
    // or fake news - worth a second look.
    Suspicious,

    // Confidently a scam attempt (phishing, advance-fee fraud, etc.).
    Scam,

    // Confidently fabricated or misleading news content.
    FakeNews
}
