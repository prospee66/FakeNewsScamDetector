using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Services;
using Moq;
using Xunit;

namespace FakeNewsScamDetector.Tests;

public class VerdictAggregatorTests
{
    [Fact]
    public async Task AnalyzeAsync_WithLowSignals_ReturnsLegitimateVerdict()
    {
        var classifier = new Mock<ITextClassifierService>();
        classifier.Setup(c => c.PredictFakeNewsScoreAsync(It.IsAny<string>())).ReturnsAsync(0.1);
        classifier.Setup(c => c.PredictScamScoreAsync(It.IsAny<string>())).ReturnsAsync(0.1);

        var urlAnalyzer = new Mock<IUrlAnalyzerService>();
        var ruleEngine = new Mock<IScamRuleEngine>();
        ruleEngine.Setup(r => r.EvaluateText(It.IsAny<string>())).Returns((0.0, new List<string>()));

        var aggregator = new VerdictAggregator(classifier.Object, urlAnalyzer.Object, ruleEngine.Object);

        var result = await aggregator.AnalyzeAsync("Let's meet for lunch tomorrow.");

        Assert.Equal(VerdictType.Legitimate, result.Verdict);
    }

    [Fact]
    public async Task AnalyzeAsync_WithHighScamSignals_ReturnsScamVerdict()
    {
        var classifier = new Mock<ITextClassifierService>();
        classifier.Setup(c => c.PredictFakeNewsScoreAsync(It.IsAny<string>())).ReturnsAsync(0.2);
        classifier.Setup(c => c.PredictScamScoreAsync(It.IsAny<string>())).ReturnsAsync(0.9);

        var urlAnalyzer = new Mock<IUrlAnalyzerService>();
        var ruleEngine = new Mock<IScamRuleEngine>();
        ruleEngine.Setup(r => r.EvaluateText(It.IsAny<string>()))
            .Returns((0.8, new List<string> { "Mentions wire transfer" }));

        var aggregator = new VerdictAggregator(classifier.Object, urlAnalyzer.Object, ruleEngine.Object);

        var result = await aggregator.AnalyzeAsync("Send a wire transfer immediately, urgent!");

        Assert.Equal(VerdictType.Scam, result.Verdict);
        Assert.Contains("Mentions wire transfer", result.Reasons);
    }
}
