namespace FakeNewsScamDetector.Services.AI;

public interface IConversationalVerifierService
{
    Task<string> AskAsync(List<ChatMessage> conversation, CancellationToken cancellationToken = default);
}
