namespace FakeNewsScamDetector.Services.ExternalApis;

public class WhoisLookupClient
{
    private readonly HttpClient _httpClient;

    public WhoisLookupClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<int?> GetDomainAgeInDaysAsync(string domain)
    {
        // Placeholder: wire up to a real WHOIS API provider and parse the
        // registration date. Returns null when unavailable so callers can
        // treat domain age as an unknown (neutral) signal.
        return Task.FromResult<int?>(null);
    }
}
