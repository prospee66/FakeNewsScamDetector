using FakeNewsScamDetector.Core.Enums;
using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FakeNewsScamDetector.Web.Controllers;

public class DashboardController : Controller
{
    private readonly IAnalysisRepository _repository;

    public DashboardController(IAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var recent = await _repository.GetRecentAsync(200);

        var model = new DashboardViewModel
        {
            TotalAnalyzed = recent.Count,
            LegitimateCount = recent.Count(r => r.Verdict == VerdictType.Legitimate),
            SuspiciousCount = recent.Count(r => r.Verdict == VerdictType.Suspicious),
            ScamCount = recent.Count(r => r.Verdict == VerdictType.Scam),
            FakeNewsCount = recent.Count(r => r.Verdict == VerdictType.FakeNews),
            RecentResults = recent.Take(20).ToList()
        };

        return View(model);
    }
}
