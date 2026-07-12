using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FakeNewsScamDetector.Services.AI;

/// <summary>
/// Conducts a conversational verification chat via the Anthropic Messages API.
/// Placeholder system prompt below — replace once the real one is supplied.
/// Ends its own turn with a "VERDICT: &lt;Legitimate|Suspicious|Scam|FakeNews&gt;"
/// line when it reaches a conclusion, which ChatController parses to decide
/// whether the conversation is complete and should be saved.
/// </summary>
public class ClaudeVerifierService : IConversationalVerifierService
{
    private const string AnthropicVersion = "2023-06-01";
    private const string MessagesEndpoint = "https://api.anthropic.com/v1/messages";

    private const string DefaultSystemPrompt = """
        You are a conversational fact-checking and scam-verification assistant
        embedded in a scam and fake-news detection tool. Help the user reason
        through a suspicious message, claim, or link by asking clarifying
        questions (who sent it, is there a link, what platform, any red flags
        they've already noticed) before drawing a conclusion.

        Do not claim certainty you do not have. Be explicit about uncertainty
        and about what would change your assessment. This is a decision-support
        conversation, not a final verdict on truth or safety — say so if the
        user seems to be treating your answer as one.

        When, and only when, you have enough information to reach a
        conclusion, end your message with exactly one line, on its own line,
        in this exact format:
        VERDICT: <Legitimate|Suspicious|Scam|FakeNews>

        Do not include that line in clarifying questions or intermediate
        messages — only in the message where you are ready to conclude.
        """;

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
                DefaultSystemPrompt,
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
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse the Claude API response.");
            return "Sorry, something went wrong reading the AI assistant's response. Please try again.";
        }
    }

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
