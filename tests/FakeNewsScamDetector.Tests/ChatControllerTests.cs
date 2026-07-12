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
    [Fact]
    public async Task Send_WithVerdictInReply_SavesAnalysisResultAndReturnsSaved()
    {
        var verifier = new Mock<IConversationalVerifierService>();
        verifier.Setup(v => v.AskAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Based on what you've described, this matches known scam patterns.\nVERDICT: Scam");

        AnalysisResult? saved = null;
        var repository = new Mock<IAnalysisRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<AnalysisResult>()))
            .Callback<AnalysisResult>(r => saved = r)
            .ReturnsAsync((AnalysisResult r) => r);

        var controller = new ChatController(verifier.Object, repository.Object, NullLogger<ChatController>.Instance);
        var conversation = new List<ChatMessage>
        {
            new() { Role = "user", Content = "Someone texted me asking for gift cards, is this a scam?" }
        };

        var result = await controller.Send(conversation, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        repository.Verify(r => r.AddAsync(It.IsAny<AnalysisResult>()), Times.Once);
        Assert.NotNull(saved);
        Assert.Equal(VerdictType.Scam, saved!.Verdict);
        Assert.Equal("Someone texted me asking for gift cards, is this a scam?", saved.InputText);
        Assert.Contains(saved.ConversationTranscript, line => line.StartsWith("user:"));
        Assert.Contains(saved.ConversationTranscript, line => line.StartsWith("assistant:"));
    }

    [Fact]
    public async Task Send_WithoutVerdictInReply_DoesNotSave()
    {
        var verifier = new Mock<IConversationalVerifierService>();
        verifier.Setup(v => v.AskAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Can you tell me more about how they contacted you?");

        var repository = new Mock<IAnalysisRepository>();

        var controller = new ChatController(verifier.Object, repository.Object, NullLogger<ChatController>.Instance);
        var conversation = new List<ChatMessage> { new() { Role = "user", Content = "Is this a scam?" } };

        await controller.Send(conversation, CancellationToken.None);

        repository.Verify(r => r.AddAsync(It.IsAny<AnalysisResult>()), Times.Never);
    }

    [Fact]
    public async Task Send_WithEmptyConversation_ReturnsBadRequest()
    {
        var verifier = new Mock<IConversationalVerifierService>();
        var repository = new Mock<IAnalysisRepository>();
        var controller = new ChatController(verifier.Object, repository.Object, NullLogger<ChatController>.Instance);

        var result = await controller.Send([], CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
