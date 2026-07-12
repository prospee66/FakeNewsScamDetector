namespace FakeNewsScamDetector.Core.Interfaces;

public interface IUrlAnalyzerService
{
    Task<(double Score, List<string> Reasons)> AnalyzeUrlRiskAsync(string url);
}
