using FakeNewsScamDetector.Services;
using Xunit;

namespace FakeNewsScamDetector.Tests;

public class ScamRuleEngineTests
{
    private readonly ScamRuleEngine _engine = new();

    [Fact]
    public void EvaluateText_WithNoScamKeywords_ReturnsZeroScore()
    {
        var (score, reasons) = _engine.EvaluateText("Let's meet for coffee tomorrow.");

        Assert.Equal(0, score);
        Assert.Empty(reasons);
    }

    [Fact]
    public void EvaluateText_WithScamKeywords_ReturnsPositiveScoreAndReasons()
    {
        var (score, reasons) = _engine.EvaluateText("Send a gift card now, this is urgent!");

        Assert.True(score > 0);
        Assert.NotEmpty(reasons);
    }

    [Fact]
    public void EvaluateText_ScoreIsCappedAtOne()
    {
        var text = string.Join(" ", new[]
        {
            "wire transfer", "gift card", "urgent", "act now", "verify your account",
            "suspended", "lottery", "inheritance", "social security number", "bitcoin",
            "click here immediately", "congratulations you have been selected"
        });

        var (score, _) = _engine.EvaluateText(text);

        Assert.Equal(1.0, score);
    }
}
