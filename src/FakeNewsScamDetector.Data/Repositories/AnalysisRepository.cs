using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FakeNewsScamDetector.Data.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AppDbContext _context;

    public AnalysisRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisResult> AddAsync(AnalysisResult result)
    {
        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<AnalysisResult?> GetByIdAsync(int id)
    {
        return await _context.AnalysisResults.FindAsync(id);
    }

    public async Task<List<AnalysisResult>> GetRecentAsync(int count = 50)
    {
        return await _context.AnalysisResults
            .OrderByDescending(a => a.AnalyzedAtUtc)
            .Take(count)
            .ToListAsync();
    }
}
