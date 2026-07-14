using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FakeNewsScamDetector.Services.ExternalApis;

// Hits Google's Fact Check Tools API to see if anyone (Snopes, PolitiFact,
// etc.) has already reviewed a similar claim. We don't try to judge truth
// ourselves here - just surface what's been published and let the user read
// it. Most input won't match anything since the API only covers claims that
// were formally reviewed, and that's fine - no match just means "nobody's
// fact-checked this one," not that it's true.
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
