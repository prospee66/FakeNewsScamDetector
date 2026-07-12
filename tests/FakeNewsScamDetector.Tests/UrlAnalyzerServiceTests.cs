using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Services;
using Moq;
using Xunit;

namespace FakeNewsScamDetector.Tests;

public class UrlAnalyzerServiceTests
{
    private readonly Mock<IWhoisLookupClient> _whoisClient = new();
    private readonly Mock<ISafeBrowsingClient> _safeBrowsingClient = new();
    private readonly UrlAnalyzerService _analyzer;

    public UrlAnalyzerServiceTests()
    {
        // Defaults: WHOIS returns "unknown" and Safe Browsing reports "not
        // flagged", so tests are fast, deterministic, and isolated from the
        // real network/API.
        _whoisClient.Setup(w => w.GetDomainAgeInDaysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);
        _safeBrowsingClient.Setup(s => s.CheckUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafeBrowsingResult(false, []));
        _analyzer = new UrlAnalyzerService(_whoisClient.Object, _safeBrowsingClient.Object);
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithHttpsRegularDomain_ReturnsLowRisk()
    {
        var (risk, _) = await _analyzer.AnalyzeUrlRiskAsync("https://www.example.com/page");

        Assert.True(risk < 0.3);
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithHttpAndSuspiciousTld_ReturnsHigherRisk()
    {
        var (risk, reasons) = await _analyzer.AnalyzeUrlRiskAsync("http://free-prize-winner.xyz");

        Assert.True(risk > 0.3);
        Assert.NotEmpty(reasons);
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithInvalidUrl_ReturnsNeutralScore()
    {
        var (risk, reasons) = await _analyzer.AnalyzeUrlRiskAsync("not a url");

        Assert.Equal(0.5, risk);
        Assert.Empty(reasons);
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithFreshlyRegisteredDomain_AddsRiskAndReason()
    {
        _whoisClient.Setup(w => w.GetDomainAgeInDaysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var (risk, reasons) = await _analyzer.AnalyzeUrlRiskAsync("https://www.example.com");

        Assert.True(risk > 0);
        Assert.Contains(reasons, r => r.Contains("5 day"));
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithOldDomain_AddsNoAgeRisk()
    {
        _whoisClient.Setup(w => w.GetDomainAgeInDaysAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3650);

        var (risk, reasons) = await _analyzer.AnalyzeUrlRiskAsync("https://www.example.com");

        Assert.Equal(0, risk);
        Assert.Empty(reasons);
    }

    [Fact]
    public async Task AnalyzeUrlRiskAsync_WithSafeBrowsingFlagged_AddsHighRiskAndReason()
    {
        _safeBrowsingClient.Setup(s => s.CheckUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SafeBrowsingResult(true, ["SOCIAL_ENGINEERING"]));

        var (risk, reasons) = await _analyzer.AnalyzeUrlRiskAsync("https://www.example.com");

        Assert.True(risk >= 0.6);
        Assert.Contains(reasons, r => r.Contains("Safe Browsing") && r.Contains("SOCIAL_ENGINEERING"));
    }
}
