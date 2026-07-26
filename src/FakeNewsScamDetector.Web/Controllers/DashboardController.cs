using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FakeNewsScamDetector.Web.Controllers;

// Summary stats + a recent-results table, shown at "/Dashboard".
public class DashboardController : Controller
{
    private readonly IAnalysisRepository _repository;

    public DashboardController(IAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        // Note: "TotalAnalyzed" below is really "count of the last 200
        // results", not a true all-time total - there's no separate COUNT
        // query against the whole table. Fine for a dashboard at this
        // scale, but worth knowing if the numbers ever look off.
        var recent = await _repository.GetRecentAsync(200);

        var model = new DashboardViewModel
        {
            TotalAnalyzed = recent.Count,
            LegitimateCount = recent.Count(r => r.Verdict == VerdictType.Legitimate),
            SuspiciousCount = recent.Count(r => r.Verdict == VerdictType.Suspicious),
            ScamCount = recent.Count(r => r.Verdict == VerdictType.Scam),
            FakeNewsCount = recent.Count(r => r.Verdict == VerdictType.FakeNews),
            // The table on screen only shows the most recent 20, even
            // though the counts above are based on up to 200.
            RecentResults = recent.Take(20).ToList()
        };

        return View(model);
    }
}
