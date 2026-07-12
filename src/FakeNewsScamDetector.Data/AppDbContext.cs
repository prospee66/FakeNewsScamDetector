using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Data.SeedData;
using Microsoft.EntityFrameworkCore;

namespace FakeNewsScamDetector.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
    public DbSet<UserFeedback> UserFeedbacks => Set<UserFeedback>();
    public DbSet<ScamPattern> ScamPatterns => Set<ScamPattern>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisResult>()
            .Property(a => a.Reasons)
            .HasConversion(
                reasons => string.Join('|', reasons),
                value => value.Length == 0
                    ? new List<string>()
                    : value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

        ScamPatternSeed.Seed(modelBuilder);
    }
}
