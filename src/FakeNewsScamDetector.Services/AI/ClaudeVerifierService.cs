using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FakeNewsScamDetector.Services.AI;

// Talks to the Anthropic Messages API. Not registered in Program.cs right now
// since Claude needs a paid credit balance and Gemini doesn't - left in place
// in case that changes later, still passes its own tests.
public class ClaudeVerifierService : IConversationalVerifierService
{
    private const string AnthropicVersion = "2023-06-01";
    private const string MessagesEndpoint = "https://api.anthropic.com/v1/messages";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeVerifierService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;

    public ClaudeVerifierService(HttpClient httpClient, IConfiguration configuration, ILogger<ClaudeVerifierService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Claude:ApiKey"];
        _model = configuration["Claude:Model"] ?? "claude-sonnet-5";
    }

    public async Task<string> AskAsync(List<ChatMessage> conversation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Claude:ApiKey is not configured; returning fallback message instead of calling the API.");
            return "The AI verification assistant isn't configured yet. Please contact the site administrator.";
        }

        try
        {
            var request = new AnthropicRequest(
                _model,
                1024,
                VerificationSystemPrompt.Text,
                conversation.Select(m => new AnthropicMessage(m.Role, m.Content)).ToList());

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, MessagesEndpoint);
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
            httpRequest.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadFromJsonAsync<AnthropicErrorBody>(cancellationToken: cancellationToken);
                _logger.LogError(
                    "Claude API returned {StatusCode}: {ErrorType} - {ErrorMessage}",
                    (int)response.StatusCode, errorBody?.Error?.Type, errorBody?.Error?.Message);
                return "Sorry, the AI verification assistant is temporarily unavailable. Please try again shortly.";
            }

            var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: cancellationToken);
            var textBlock = result?.Content?.FirstOrDefault(b => b.Type == "text" && b.Text is not null);

            if (textBlock is null)
            {
                _logger.LogWarning("Claude API response contained no text content block.");
                return "Sorry, I couldn't generate a response just now. Please try again.";
            }

            return textBlock.Text!;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling the Claude API.");
            return "Sorry, I couldn't reach the AI verification assistant. Please try again shortly.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The caller didn't cancel - this is HttpClient.Timeout firing, which
            // surfaces as a TaskCanceledException, not an HttpRequestException.
            _logger.LogError("Claude API call timed out.");
            return "Sorry, the AI verification assistant is taking too long to respond. Please try again shortly.";
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse the Claude API response.");
            return "Sorry, something went wrong reading the AI assistant's response. Please try again.";
        }
    }

    // Private DTOs below just mirror the shape of Anthropic's Messages API
    // request/response JSON - see Anthropic's API docs for the full schema.
    private record AnthropicRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] List<AnthropicMessage> Messages);

    private record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record AnthropicContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);

    private record AnthropicResponse(
        [property: JsonPropertyName("content")] List<AnthropicContentBlock>? Content);

    private record AnthropicErrorDetail(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("message")] string? Message);

    private record AnthropicErrorBody(
        [property: JsonPropertyName("error")] AnthropicErrorDetail? Error);
}
