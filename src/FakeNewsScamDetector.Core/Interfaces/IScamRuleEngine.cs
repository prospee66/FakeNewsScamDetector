namespace FakeNewsScamDetector.Core.Interfaces;

// Keyword/phrase based scam detection - fast, deterministic, and cheap
// compared to the ML model. Implemented by ScamRuleEngine.
public interface IScamRuleEngine
{
    // Scans the given text for known scam red flags. Returns a 0-1 score
    // (higher = more suspicious) plus the human-readable reasons behind it,
    // so the UI can show *why* something was flagged, not just a number.
    (double Score, List<string> MatchedReasons) EvaluateText(string text);
}
