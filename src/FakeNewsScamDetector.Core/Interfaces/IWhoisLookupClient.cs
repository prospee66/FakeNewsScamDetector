namespace FakeNewsScamDetector.Core.Interfaces;

// Looks up how long ago a domain was registered, via raw WHOIS (no API key
// needed). Implemented by WhoisLookupClient. A freshly-registered domain is
// a common scam signal.
public interface IWhoisLookupClient
{
    // Null means "couldn't determine this" (network failure, unparseable
    // WHOIS response, etc.) - not "the domain has no age".
    Task<int?> GetDomainAgeInDaysAsync(string domain, CancellationToken cancellationToken = default);
}
