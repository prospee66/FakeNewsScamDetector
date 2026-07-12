namespace FakeNewsScamDetector.Core.Interfaces;

public interface ITextClassifierService
{
    Task<double> PredictFakeNewsScoreAsync(string text);
    Task<double> PredictScamScoreAsync(string text);
}
