using FakeNewsScamDetector.Core.Entities;

namespace FakeNewsScamDetector.Core.Interfaces;

// Read/write access to saved scan results. Implemented by
// AnalysisRepository (Entity Framework Core + SQLite) in the Data project -
// controllers depend on this interface, not the EF implementation directly,
// so the storage could be swapped out without touching controller code.
public interface IAnalysisRepository
{
    // Saves a new result and returns it with its generated Id populated.
    Task<AnalysisResult> AddAsync(AnalysisResult result);

    // Looks up one result by id, or null if it doesn't exist (used by the
    // Result page after a scan, and when viewing History).
    Task<AnalysisResult?> GetByIdAsync(int id);

    // Most recent results first, for the History and Dashboard pages.
    Task<List<AnalysisResult>> GetRecentAsync(int count = 50);
}
