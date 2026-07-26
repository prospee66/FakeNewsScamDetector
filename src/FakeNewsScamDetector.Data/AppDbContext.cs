using System.Text.Json;
using FakeNewsScamDetector.Core.Entities;
using FakeNewsScamDetector.Data.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FakeNewsScamDetector.Data;

// The single EF Core context for the whole app - one SQLite database
// (see the "DefaultConnection" connection string in appsettings.json),
// created automatically at startup via EnsureCreated() in Program.cs.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Saved scan results - the main table the app actually reads and
    // writes through AnalysisRepository.
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    // Reserved for a "was this verdict accurate?" feedback feature that
    // doesn't have a UI yet - see the comment on UserFeedback.
    public DbSet<UserFeedback> UserFeedbacks => Set<UserFeedback>();

    // Seeded reference data - see the comment on ScamPattern for why this
    // isn't actually queried by the live rule engine yet.
    public DbSet<ScamPattern> ScamPatterns => Set<ScamPattern>();

    // SQLite doesn't support any of the List<T> properties on AnalysisResult
    // natively, so each one needs an explicit conversion to/from a plain
    // string column. Reasons uses a simple pipe-delimited join since reason
    // text is app-generated and predictable; the other two use JSON because
    // they can contain arbitrary characters (chat text, claim text) that
    // could include a literal "|" and break a pipe-delimited split.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisResult>()
            .Property(a => a.Reasons)
            .HasConversion(
                reasons => string.Join('|', reasons),
                value => value.Length == 0
                    ? new List<string>()
                    : value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList())
            // EF needs an explicit value comparer for converted collection
            // properties, otherwise change-tracking can't tell whether the
            // list actually changed between saves.
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

        modelBuilder.Entity<AnalysisResult>()
            .Property(a => a.ConversationTranscript)
            .HasConversion(
                lines => JsonSerializer.Serialize(lines, (JsonSerializerOptions?)null),
                json => string.IsNullOrEmpty(json)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => v.ToList()));

        ScamPatternSeed.Seed(modelBuilder);
    }
}
