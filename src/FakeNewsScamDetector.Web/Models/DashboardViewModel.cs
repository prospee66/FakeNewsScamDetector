using FakeNewsScamDetector.Core.Entities;

namespace FakeNewsScamDetector.Web.Models;

public class DashboardViewModel
{
    public int TotalAnalyzed { get; set; }
    public int LegitimateCount { get; set; }
    public int SuspiciousCount { get; set; }
    public int ScamCount { get; set; }
    public int FakeNewsCount { get; set; }
    public List<AnalysisResult> RecentResults { get; set; } = new();
}
