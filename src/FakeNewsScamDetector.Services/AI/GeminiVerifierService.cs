using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FakeNewsScamDetector.Services.AI;

// Talks to Gemini's generateContent endpoint. Using Gemini instead of Claude
// here because it has a free tier - swapping this out for ClaudeVerifierService
// later shouldn't require touching ChatController at all.
public class GeminiVerifierService : IConversationalVerifierService
{
    private const string EndpointTemplate = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiVerifierService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;

    public GeminiVerifierService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiVerifierService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"];
        _model = configuration["Gemini:Model"] ?? "gemini-flash-latest";
    }

    public async Task<string> AskAsync(List<ChatMessage> conversation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini:ApiKey is not configured; returning fallback message instead of calling the API.");
            return "The AI verification assistant isn't configured yet. Please contact the site administrator.";
        }

        try
        {
            var request = new GeminiRequest(
                conversation.Select(ToGeminiContent).ToList(),
                new GeminiSystemInstruction([new GeminiPart(VerificationSystemPrompt.Text)]));

            var url = string.Format(EndpointTemplate, _model, Uri.EscapeDataString(_apiKey));

            using var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadFromJsonAsync<GeminiErrorBody>(cancellationToken: cancellationToken);
                _logger.LogError(
                    "Gemini API returned {StatusCode}: {ErrorMessage}",
                    (int)response.StatusCode, errorBody?.Error?.Message);
                return "Sorry, the AI verification assistant is temporarily unavailable. Please try again shortly.";
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
            var text = result?.Candidates?
                .FirstOrDefault()?.Content?.Parts?
                .FirstOrDefault(p => p.Text is not null)?.Text;

            if (text is null)
            {
                _logger.LogWarning("Gemini API response contained no text content.");
                return "Sorry, I couldn't generate a response just now. Please try again.";
            }

            return text;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling the Gemini API.");
            return "Sorry, I couldn't reach the AI verification assistant. Please try again shortly.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The caller didn't cancel - this is HttpClient.Timeout firing, which
            // surfaces as a TaskCanceledException, not an HttpRequestException.
            _logger.LogError("Gemini API call timed out.");
            return "Sorry, the AI verification assistant is taking too long to respond. Please try again shortly.";
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse the Gemini API response.");
            return "Sorry, something went wrong reading the AI assistant's response. Please try again.";
        }
    }

    private static GeminiContent ToGeminiContent(ChatMessage message)
    {
        // Gemini uses "model" for the assistant role where Claude/OpenAI use
        // "assistant" — translate here so ChatMessage stays provider-neutral.
        var role = message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
        return new GeminiContent(role, [new GeminiPart(message.Content)]);
    }

    // Private DTOs below just mirror the shape of Gemini's generateContent
    // request/response JSON - see Google's API docs for the full schema.
    // Kept private since nothing outside this class needs them.
    private record GeminiPart([property: JsonPropertyName("text")] string Text);

    private record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

    private record GeminiSystemInstruction(
        [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

    private record GeminiRequest(
        [property: JsonPropertyName("contents")] List<GeminiContent> Contents,
        [property: JsonPropertyName("systemInstruction")] GeminiSystemInstruction SystemInstruction);

    private record GeminiResponseContent(
        [property: JsonPropertyName("parts")] List<GeminiPart>? Parts);

    private record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiResponseContent? Content);

    private record GeminiResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

    private record GeminiErrorDetail(
        [property: JsonPropertyName("message")] string? Message);

    private record GeminiErrorBody(
        [property: JsonPropertyName("error")] GeminiErrorDetail? Error);
}
