using FakeNewsScamDetector.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FakeNewsScamDetector.Tests;

public class ClaudeVerifierServiceTests
{
    [Fact]
    public async Task AskAsync_WithoutApiKeyConfigured_ReturnsFallbackMessageWithoutCallingApi()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var service = new ClaudeVerifierService(new HttpClient(), config, NullLogger<ClaudeVerifierService>.Instance);

        var reply = await service.AskAsync([new ChatMessage { Role = "user", Content = "Is this a scam?" }]);

        Assert.Contains("isn't configured", reply, StringComparison.OrdinalIgnoreCase);
    }
}
