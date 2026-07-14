using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Services.AI;
using FakeNewsScamDetector.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FakeNewsScamDetector.Tests;

public class ChatControllerTests
{
    private static ChatController CreateController(Mock<IConversationalVerifierService> verifier, Mock<IAnalysisRepository> repository) =>
        new(verifier.Object, repository.Object, NullLogger<ChatController>.Instance);

    [Fact]
    public async Task SavesToHistoryWhenReplyContainsVerdict()
    {
        var verifier = new Mock<IConversationalVerifierService>();
        verifier.Setup(v => v.AskAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Based on what you've described, this matches known scam patterns.\nVERDICT: Scam");

        AnalysisResult? saved = null;
        var repository = new Mock<IAnalysisRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<AnalysisResult>()))
            .Callback<AnalysisResult>(r => saved = r)
            .ReturnsAsync((AnalysisResult r) => r);

        var conversation = new List<ChatMessage>
        {
            new() { Role = "user", Content = "Someone texted me asking for gift cards, is this a scam?" }
        };

        await CreateController(verifier, repository).Send(conversation, CancellationToken.None);

        repository.Verify(r => r.AddAsync(It.IsAny<AnalysisResult>()), Times.Once);
        Assert.NotNull(saved);
        Assert.Equal(VerdictType.Scam, saved!.Verdict);
        Assert.Equal("Someone texted me asking for gift cards, is this a scam?", saved.InputText);
        // both sides of the conversation should end up in the transcript, not just the verdict line
        Assert.Contains(saved.ConversationTranscript, line => line.StartsWith("user:"));
        Assert.Contains(saved.ConversationTranscript, line => line.StartsWith("assistant:"));
    }

    [Fact]
    public async Task DoesNotSaveWhenNoVerdictYet()
    {
        var verifier = new Mock<IConversationalVerifierService>();
        verifier.Setup(v => v.AskAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Can you tell me more about how they contacted you?");
        var repository = new Mock<IAnalysisRepository>();

        var conversation = new List<ChatMessage> { new() { Role = "user", Content = "Is this a scam?" } };
        await CreateController(verifier, repository).Send(conversation, CancellationToken.None);

        repository.Verify(r => r.AddAsync(It.IsAny<AnalysisResult>()), Times.Never);
    }

    [Fact]
    public async Task RejectsEmptyConversation()
    {
        var verifier = new Mock<IConversationalVerifierService>();
        var repository = new Mock<IAnalysisRepository>();

        var result = await CreateController(verifier, repository).Send([], CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
