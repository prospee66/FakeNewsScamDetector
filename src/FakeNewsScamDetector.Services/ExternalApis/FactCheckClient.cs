using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FakeNewsScamDetector.Services.ExternalApis;

/// <summary>
/// Searches Google's Fact Check Tools API (claims:search) for existing,
/// human-written fact-check reviews matching the input text. This is
/// deliberately *not* a classifier trying to decide truth itself — it
/// surfaces what professional fact-checkers (Snopes, PolitiFact, Reuters,
/// etc.) have already published about similar claims, verbatim, so the user
/// can judge the source and rating themselves. Most arbitrary input won't
/// match anything (the API only indexes claims that have been formally
/// reviewed), which is itself meaningful: no match means "no one has
/// fact-checked this specific claim," not "this is true."
/// </summary>
public class FactCheckClient : IFactCheckClient
{
    private const int MaxQueryLength = 200;
    private const int MaxResults = 5;

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public FactCheckClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["FactCheck:ApiKey"];
    }

    public async Task<List<FactCheckFinding>> SearchClaimsAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(text))
            return [];

        try
        {
            var query = text.Length > MaxQueryLength ? text[..MaxQueryLength] : text;
            var url = $"https://factchecktools.googleapis.com/v1alpha1/claims:search" +
                      $"?query={Uri.EscapeDataString(query)}&languageCode=en&pageSize={MaxResults}&key={Uri.EscapeDataString(_apiKey)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            var result = await response.Content.ReadFromJsonAsync<ClaimSearchResponse>(cancellationToken);
            if (result?.Claims is null)
                return [];

            return result.Claims
                .Where(c => c.ClaimReview is not null)
                .SelectMany(c => c.ClaimReview!.Select(r => new FactCheckFinding
                {
                    ClaimText = c.Text ?? string.Empty,
                    Publisher = r.Publisher?.Name ?? "Unknown publisher",
                    TextualRating = r.TextualRating ?? "Unrated",
                    ReviewUrl = r.Url ?? string.Empty
                }))
                .Take(MaxResults)
                .ToList();
        }
        catch
        {
            // Network failure or malformed response: no findings, not a
            // false "nothing was found" claim of truth.
            return [];
        }
    }

    private record Publisher([property: JsonPropertyName("name")] string? Name);

    private record ClaimReview(
        [property: JsonPropertyName("publisher")] Publisher? Publisher,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("textualRating")] string? TextualRating);

    private record Claim(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("claimReview")] List<ClaimReview>? ClaimReview);

    private record ClaimSearchResponse([property: JsonPropertyName("claims")] List<Claim>? Claims);
}
