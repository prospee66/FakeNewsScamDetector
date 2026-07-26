using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Services;
using FakeNewsScamDetector.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FakeNewsScamDetector.Web.Controllers;

// The "Analyze Text or URL" scored-pipeline feature: submit text, run it
// through VerdictAggregator, save the result, show it. This is the main
// flow of the app - separate from the free-form AI Chat flow (ChatController).
public class AnalysisController : Controller
{
    private readonly VerdictAggregator _aggregator;
    private readonly IAnalysisRepository _repository;

    public AnalysisController(VerdictAggregator aggregator, IAnalysisRepository repository)
    {
        _aggregator = aggregator;
        _repository = repository;
    }

    // GET /Analysis - the empty input form.
    [HttpGet]
    public IActionResult Index()
    {
        return View(new AnalyzeRequestViewModel());
    }

    // POST /Analysis/Analyze - runs the scan and redirects to its result
    // page (POST-redirect-GET, so refreshing the result page doesn't
    // resubmit the form and create a duplicate scan).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(AnalyzeRequestViewModel request)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), request);

        // HttpContext.RequestAborted is passed through so that if the user
        // navigates away mid-scan, the external API calls inside
        // AnalyzeAsync (WHOIS, Safe Browsing, Fact Check) get cancelled
        // instead of running to completion for no one.
        var result = await _aggregator.AnalyzeAsync(request.InputText, HttpContext.RequestAborted);
        var saved = await _repository.AddAsync(result);

        return RedirectToAction(nameof(Result), new { id = saved.Id });
    }

    // GET /Analysis/Result/{id} - shows one saved scan's full breakdown.
    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var result = await _repository.GetByIdAsync(id);
        if (result is null)
            return NotFound();

        return View(result);
    }

    // GET /Analysis/History - a table of past scans, most recent first.
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var results = await _repository.GetRecentAsync();
        return View(results);
    }
}
