namespace FakeNewsScamDetector.Core.Interfaces;

public record SafeBrowsingResult(bool IsFlagged, List<string> ThreatTypes);

public interface ISafeBrowsingClient
{
    Task<SafeBrowsingResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default);
}
