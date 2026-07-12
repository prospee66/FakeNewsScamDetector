using FakeNewsScamDetector.Core.Entities;
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

        var factCheckClient = new Mock<IFactCheckClient>();
        factCheckClient.Setup(f => f.SearchClaimsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FactCheckFinding>());

        var aggregator = new VerdictAggregator(classifier.Object, urlAnalyzer.Object, ruleEngine.Object, factCheckClient.Object);

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

        var factCheckClient = new Mock<IFactCheckClient>();
        factCheckClient.Setup(f => f.SearchClaimsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FactCheckFinding>());

        var aggregator = new VerdictAggregator(classifier.Object, urlAnalyzer.Object, ruleEngine.Object, factCheckClient.Object);

        var result = await aggregator.AnalyzeAsync("Send a wire transfer immediately, urgent!");

        Assert.Equal(VerdictType.Scam, result.Verdict);
        Assert.Contains("Mentions wire transfer", result.Reasons);
    }

    [Fact]
    public async Task AnalyzeAsync_WithFactCheckFindings_PopulatesFindingsWithoutAffectingScore()
    {
        var classifier = new Mock<ITextClassifierService>();
        classifier.Setup(c => c.PredictFakeNewsScoreAsync(It.IsAny<string>())).ReturnsAsync(0.1);
        classifier.Setup(c => c.PredictScamScoreAsync(It.IsAny<string>())).ReturnsAsync(0.1);

        var urlAnalyzer = new Mock<IUrlAnalyzerService>();
        var ruleEngine = new Mock<IScamRuleEngine>();
        ruleEngine.Setup(r => r.EvaluateText(It.IsAny<string>())).Returns((0.0, new List<string>()));

        var finding = new FactCheckFinding
        {
            ClaimText = "Example claim",
            Publisher = "Example Fact Checkers",
            TextualRating = "False",
            ReviewUrl = "https://example.com/review"
        };
        var factCheckClient = new Mock<IFactCheckClient>();
        factCheckClient.Setup(f => f.SearchClaimsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FactCheckFinding> { finding });

        var aggregator = new VerdictAggregator(classifier.Object, urlAnalyzer.Object, ruleEngine.Object, factCheckClient.Object);

        var result = await aggregator.AnalyzeAsync("Some claim to check.");

        Assert.Single(result.FactCheckFindings);
        Assert.Equal("False", result.FactCheckFindings[0].TextualRating);
        // Low ML/rule/url signals should still resolve to Legitimate — a
        // fact-check finding must never silently override the transparent
        // score breakdown.
        Assert.Equal(VerdictType.Legitimate, result.Verdict);
    }
}
