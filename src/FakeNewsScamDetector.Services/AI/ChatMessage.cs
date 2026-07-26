namespace FakeNewsScamDetector.Services.AI;

// One turn in an AI chat conversation. Provider-neutral on purpose - Role is
// always "user" or "assistant" here, even though Gemini's API calls the
// assistant role "model" internally (GeminiVerifierService translates
// between the two so the rest of the app never has to know).
public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
