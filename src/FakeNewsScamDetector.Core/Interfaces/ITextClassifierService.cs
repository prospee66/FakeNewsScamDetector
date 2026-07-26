namespace FakeNewsScamDetector.Core.Interfaces;

// Wraps the two trained ML.NET models (fake-news and scam-text
// classifiers). Implemented by TextClassifierService in the ML project -
// kept behind an interface here so the Services/Web layers don't need a
// direct reference to ML.NET types.
public interface ITextClassifierService
{
    // 0-1 probability the given text resembles known fake news.
    Task<double> PredictFakeNewsScoreAsync(string text);

    // 0-1 probability the given text resembles known scam messaging.
    Task<double> PredictScamScoreAsync(string text);
}
