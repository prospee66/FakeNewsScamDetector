namespace FakeNewsScamDetector.Core.Interfaces;

// Structural + reputation risk-scoring for a single URL (suspicious TLDs,
// URL shorteners, domain age via WHOIS, Google Safe Browsing, etc.).
// Implemented by UrlAnalyzerService.
public interface IUrlAnalyzerService
{
    // Returns a 0-1 risk score for the given URL (higher = riskier) plus
    // the specific reasons behind it, so the UI can explain the score
    // instead of just showing a bare number.
    Task<(double Score, List<string> Reasons)> AnalyzeUrlRiskAsync(string url);
}
