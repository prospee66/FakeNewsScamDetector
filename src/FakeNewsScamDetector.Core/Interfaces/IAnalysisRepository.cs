using FakeNewsScamDetector.Core.Entities;

namespace FakeNewsScamDetector.Core.Interfaces;

public interface IAnalysisRepository
{
    Task<AnalysisResult> AddAsync(AnalysisResult result);
    Task<AnalysisResult?> GetByIdAsync(int id);
    Task<List<AnalysisResult>> GetRecentAsync(int count = 50);
}
