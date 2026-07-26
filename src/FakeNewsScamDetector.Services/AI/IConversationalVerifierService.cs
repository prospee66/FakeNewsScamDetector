namespace FakeNewsScamDetector.Services.AI;

// A conversational LLM backend for the AI Chat feature (ChatController).
// Implemented by GeminiVerifierService (currently wired up, since Gemini
// has a free tier) and ClaudeVerifierService (written and tested, but not
// registered in Program.cs - swap the registration to switch providers).
public interface IConversationalVerifierService
{
    // Sends the whole conversation so far and returns the assistant's next
    // reply. Never throws for a failed API call/timeout - implementations
    // catch those and return a user-facing fallback message instead.
    Task<string> AskAsync(List<ChatMessage> conversation, CancellationToken cancellationToken = default);
}
