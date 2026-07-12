using System.Text.Json;
using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Data.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
                    : value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        modelBuilder.Entity<AnalysisResult>()
            .Property(a => a.FactCheckFindings)
            .HasConversion(
                findings => JsonSerializer.Serialize(findings, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json)
                    ? new List<FactCheckFinding>()
                    : JsonSerializer.Deserialize<List<FactCheckFinding>>(json, (JsonSerializerOptions?)null) ?? new List<FactCheckFinding>())
            .Metadata.SetValueComparer(new ValueComparer<List<FactCheckFinding>>(
                (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                v => v.Aggregate(0, (hash, f) => HashCode.Combine(hash, f.ClaimText, f.Publisher, f.TextualRating, f.ReviewUrl)),
                v => v.ToList()));

        ScamPatternSeed.Seed(modelBuilder);
    }
}
