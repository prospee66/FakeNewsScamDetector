using FakeNewsScamDetector.ML.Prediction;
using Xunit;

namespace FakeNewsScamDetector.Tests;

public class TextClassifierServiceTests
{
    [Fact]
    public async Task PredictFakeNewsScoreAsync_WithoutTrainedModel_ReturnsNeutralScore()
    {
        var service = new TextClassifierService("nonexistent-fake-news-model.zip", "nonexistent-scam-model.zip");

        var score = await service.PredictFakeNewsScoreAsync("Some sample text.");

        Assert.Equal(0.5, score);
    }

    [Fact]
    public async Task PredictScamScoreAsync_WithoutTrainedModel_ReturnsNeutralScore()
    {
        var service = new TextClassifierService("nonexistent-fake-news-model.zip", "nonexistent-scam-model.zip");

        var score = await service.PredictScamScoreAsync("Some sample text.");

        Assert.Equal(0.5, score);
    }
}
