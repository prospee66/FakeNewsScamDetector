using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FakeNewsScamDetector.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FakeNewsScamDetector.Services.ExternalApis;

/// <summary>
/// Checks a URL against Google Safe Browsing's threatMatches:find endpoint
/// (v4 Lookup API). Requires an API key (see appsettings "SafeBrowsing:ApiKey",
/// normally supplied via `dotnet user-secrets` — never commit a real key).
/// Without a key configured, every check reports "not flagged" so the app
/// still runs; this is a genuine "we didn't check", not evidence of safety.
/// </summary>
public class SafeBrowsingClient : ISafeBrowsingClient
{
    private static readonly string[] ThreatTypes =
        ["MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION"];

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public SafeBrowsingClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["SafeBrowsing:ApiKey"];
    }

    public async Task<SafeBrowsingResult> CheckUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new SafeBrowsingResult(false, []);

        try
        {
            var request = new ThreatMatchesRequest(
                new ClientInfo("FakeNewsScamDetector", "1.0.0"),
                new ThreatInfo(
                    ThreatTypes,
                    ["ANY_PLATFORM"],
                    ["URL"],
                    [new ThreatEntry(url)]));

            var response = await _httpClient.PostAsJsonAsync(
                $"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={Uri.EscapeDataString(_apiKey)}",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new SafeBrowsingResult(false, []);

            var result = await response.Content.ReadFromJsonAsync<ThreatMatchesResponse>(cancellationToken);
            var threatTypes = result?.Matches?.Select(m => m.ThreatType).Distinct().ToList() ?? [];
            return new SafeBrowsingResult(threatTypes.Count > 0, threatTypes);
        }
        catch
        {
            // Network failure or malformed response: treat as "didn't check",
            // not "confirmed safe".
            return new SafeBrowsingResult(false, []);
        }
    }

    private record ClientInfo(string ClientId, string ClientVersion);
    private record ThreatEntry(string Url);
    private record ThreatInfo(string[] ThreatTypes, string[] PlatformTypes, string[] ThreatEntryTypes, ThreatEntry[] ThreatEntries);
    private record ThreatMatchesRequest(ClientInfo Client, ThreatInfo ThreatInfo);

    private record ThreatMatch([property: JsonPropertyName("threatType")] string ThreatType);
    private record ThreatMatchesResponse([property: JsonPropertyName("matches")] List<ThreatMatch>? Matches);
}
