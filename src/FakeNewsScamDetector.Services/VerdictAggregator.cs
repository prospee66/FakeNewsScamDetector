using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services;

public class VerdictAggregator
{
    private readonly ITextClassifierService _classifier;
    private readonly IUrlAnalyzerService _urlAnalyzer;
    private readonly IScamRuleEngine _ruleEngine;
    private readonly IFactCheckClient _factCheckClient;

    public VerdictAggregator(
        ITextClassifierService classifier,
        IUrlAnalyzerService urlAnalyzer,
        IScamRuleEngine ruleEngine,
        IFactCheckClient factCheckClient)
    {
        _classifier = classifier;
        _urlAnalyzer = urlAnalyzer;
        _ruleEngine = ruleEngine;
        _factCheckClient = factCheckClient;
    }

    public async Task<AnalysisResult> AnalyzeAsync(string inputText, CancellationToken cancellationToken = default)
    {
        var normalized = TextPreprocessor.Normalize(inputText);
        var url = TextPreprocessor.ExtractFirstUrl(normalized);
        var textOnly = url is null ? normalized : TextPreprocessor.StripUrls(normalized);

        var (ruleScore, ruleReasons) = _ruleEngine.EvaluateText(textOnly);

        // These don't depend on each other, so run them concurrently instead of
        // waiting on the ML scores, then the URL check, then the fact-check API
        // one after another - that serial chain was the main source of latency.
        var fakeNewsTask = _classifier.PredictFakeNewsScoreAsync(textOnly);
        var scamTask = _classifier.PredictScamScoreAsync(textOnly);
        var urlTask = url is not null
            ? _urlAnalyzer.AnalyzeUrlRiskAsync(url)
            : Task.FromResult((0.0, new List<string>()));
        // not folding fact-check ratings into the score - publishers use
        // different rating scales ("Pants on Fire", "Mixture", etc.) that
        // don't map cleanly onto a fake/not-fake number, so we just show
        // them as-is on the result page instead of guessing
        var factCheckTask = _factCheckClient.SearchClaimsAsync(textOnly, cancellationToken);

        await Task.WhenAll(fakeNewsTask, scamTask, urlTask, factCheckTask);

        var fakeNewsScore = fakeNewsTask.Result;
        var scamScore = scamTask.Result;
        var (urlRisk, urlReasons) = urlTask.Result;
        var factCheckFindings = factCheckTask.Result;

        var mlScore = Math.Max(fakeNewsScore, scamScore);
        var confidence = (mlScore * 0.5) + (ruleScore * 0.3) + (urlRisk * 0.2);

        var verdict = DetermineVerdict(confidence, fakeNewsScore, scamScore);

        var reasons = new List<string>(ruleReasons);
        reasons.AddRange(urlReasons);
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
            FactCheckFindings = factCheckFindings,
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
