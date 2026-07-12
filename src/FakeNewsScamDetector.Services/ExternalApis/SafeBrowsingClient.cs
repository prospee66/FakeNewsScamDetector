namespace FakeNewsScamDetector.Services.ExternalApis;

public class SafeBrowsingClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public SafeBrowsingClient(HttpClient httpClient, string? apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public Task<bool> IsUrlFlaggedAsync(string url)
    {
        // Placeholder: call Google Safe Browsing's threatMatches:find endpoint
        // when an API key is configured. Returns false (not flagged) otherwise.
        if (string.IsNullOrEmpty(_apiKey))
            return Task.FromResult(false);

        return Task.FromResult(false);
    }
}
