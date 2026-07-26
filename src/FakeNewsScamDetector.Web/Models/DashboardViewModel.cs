using FakeNewsScamDetector.Core.Entities;

namespace FakeNewsScamDetector.Web.Models;

// Data for the Dashboard view - see DashboardController for how the counts
// below are computed (they're based on the last 200 results, not the
// entire history table).
public class DashboardViewModel
{
    public int TotalAnalyzed { get; set; }
    public int LegitimateCount { get; set; }
    public int SuspiciousCount { get; set; }
    public int ScamCount { get; set; }
    public int FakeNewsCount { get; set; }
    public List<AnalysisResult> RecentResults { get; set; } = new();
}
