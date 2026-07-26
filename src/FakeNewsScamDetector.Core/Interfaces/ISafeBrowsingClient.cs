namespace FakeNewsScamDetector.Core.Interfaces;

// IsFlagged is false whenever ThreatTypes is empty - either the URL is
// genuinely clean, or (if no API key is configured) it just wasn't checked.
// See SafeBrowsingClient for that distinction.
public record SafeBrowsingResult(bool IsFlagged, List<string> ThreatTypes);

// Checks a URL against Google Safe Browsing's known-threat lists.
// Implemented by SafeBrowsingClient.
public interface ISafeBrowsingClient
{
    Task<SafeBrowsingResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default);
}
