using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services;

// The main scan pipeline: takes raw user input, runs it through the ML
// classifiers, rule engine, URL risk check, and fact-check lookup, then
// blends the results into a single verdict. This is what AnalysisController
// calls for the "Analyze Text or URL" feature (the AI Chat feature is a
// separate path - see ChatController - that doesn't go through here).
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

        // Take whichever ML score is higher - a message can plausibly be
        // scam-like or fake-news-like, but we only need one verdict, and
        // DetermineVerdict below uses whichever is higher to decide which.
        var mlScore = Math.Max(fakeNewsScore, scamScore);

        // Weighted blend of all three signals. ML gets the most weight
        // since it's the most informed single signal; URL risk gets the
        // least since plenty of legitimate messages don't contain a link
        // at all (urlRisk is just 0 in that case, so it doesn't drag the
        // score down unfairly).
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

    // Two hand-picked thresholds turn the continuous 0-1 confidence score
    // into one of the four VerdictType buckets. Below 0.35 is treated as
    // clean, 0.35-0.6 as worth a second look but not conclusive, and above
    // 0.6 as confident enough to call it Scam or FakeNews specifically -
    // whichever of the two ML scores was higher decides which one.
    private static VerdictType DetermineVerdict(double confidence, double fakeNewsScore, double scamScore)
    {
        if (confidence < 0.35)
            return VerdictType.Legitimate;

        if (confidence < 0.6)
            return VerdictType.Suspicious;

        return scamScore >= fakeNewsScore ? VerdictType.Scam : VerdictType.FakeNews;
    }
}
