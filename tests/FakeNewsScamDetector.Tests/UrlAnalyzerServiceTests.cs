using FakeNewsScamDetector.Services;
using Xunit;

namespace FakeNewsScamDetector.Tests;

public class UrlAnalyzerServiceTests
{
    private readonly UrlAnalyzerService _analyzer = new();

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithHttpsRegularDomain_ReturnsLowRisk()
    {
        var risk = await _analyzer.AnalyzeUrlRiskAsync("https://www.example.com/page");

        Assert.True(risk < 0.3);
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithHttpAndSuspiciousTld_ReturnsHigherRisk()
    {
        var risk = await _analyzer.AnalyzeUrlRiskAsync("http://free-prize-winner.xyz");

        Assert.True(risk > 0.3);
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithInvalidUrl_ReturnsNeutralScore()
    {
        var risk = await _analyzer.AnalyzeUrlRiskAsync("not a url");

        Assert.Equal(0.5, risk);
    }
}
