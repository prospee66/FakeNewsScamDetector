using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services;

public class VerdictAggregator
{
    private readonly ITextClassifierService _classifier;
    private readonly IUrlAnalyzerService _urlAnalyzer;
    private readonly IScamRuleEngine _ruleEngine;

    public VerdictAggregator(
        ITextClassifierService classifier,
        IUrlAnalyzerService urlAnalyzer,
        IScamRuleEngine ruleEngine)
    {
        _classifier = classifier;
        _urlAnalyzer = urlAnalyzer;
        _ruleEngine = ruleEngine;
    }

    public async Task<AnalysisResult> AnalyzeAsync(string inputText)
    {
        var normalized = TextPreprocessor.Normalize(inputText);
        var url = TextPreprocessor.ExtractFirstUrl(normalized);
        var textOnly = url is null ? normalized : TextPreprocessor.StripUrls(normalized);

        var fakeNewsScore = await _classifier.PredictFakeNewsScoreAsync(textOnly);
        var scamScore = await _classifier.PredictScamScoreAsync(textOnly);
        var (ruleScore, ruleReasons) = _ruleEngine.EvaluateText(textOnly);
        var urlRisk = url is not null ? await _urlAnalyzer.AnalyzeUrlRiskAsync(url) : 0.0;

        var mlScore = Math.Max(fakeNewsScore, scamScore);
        var confidence = (mlScore * 0.5) + (ruleScore * 0.3) + (urlRisk * 0.2);

        var verdict = DetermineVerdict(confidence, fakeNewsScore, scamScore);

        var reasons = new List<string>(ruleReasons);
        if (url is not null && urlRisk >= 0.4)
            reasons.Add($"URL '{url}' shows risk indicators (score {urlRisk:P0})");
        if (fakeNewsScore >= 0.6)
            reasons.Add($"Text patterns resemble known fake news (score {fakeNewsScore:P0})");
        if (scamScore >= 0.6)
            reasons.Add($"Text patterns resemble known scam messaging (score {scamScore:P0})");

        return new AnalysisResult
        {
            InputText = inputText,
            InputUrl = url,
            Verdict = verdict,
            ConfidenceScore = confidence,
            MlScore = mlScore,
            RuleScore = ruleScore,
            UrlRiskScore = urlRisk,
            Reasons = reasons,
            AnalyzedAtUtc = DateTime.UtcNow
        };
    }

    private static VerdictType DetermineVerdict(double confidence, double fakeNewsScore, double scamScore)
    {
        if (confidence < 0.35)
            return VerdictType.Legitimate;

        if (confidence < 0.6)
            return VerdictType.Suspicious;

        return scamScore >= fakeNewsScore ? VerdictType.Scam : VerdictType.FakeNews;
    }
}
