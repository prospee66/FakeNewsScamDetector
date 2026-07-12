namespace FakeNewsScamDetector.Core.Interfaces;

public interface IUrlAnalyzerService
{
    Task<double> AnalyzeUrlRiskAsync(string url);
}
