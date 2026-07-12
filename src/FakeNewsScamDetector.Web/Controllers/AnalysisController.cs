using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.Services;
using FakeNewsScamDetector.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FakeNewsScamDetector.Web.Controllers;

public class AnalysisController : Controller
{
    private readonly VerdictAggregator _aggregator;
    private readonly IAnalysisRepository _repository;

    public AnalysisController(VerdictAggregator aggregator, IAnalysisRepository repository)
    {
        _aggregator = aggregator;
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new AnalyzeRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(AnalyzeRequestViewModel request)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), request);

        var result = await _aggregator.AnalyzeAsync(request.InputText);
        var saved = await _repository.AddAsync(result);

        return RedirectToAction(nameof(Result), new { id = saved.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var result = await _repository.GetByIdAsync(id);
        if (result is null)
            return NotFound();

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var results = await _repository.GetRecentAsync();
        return View(results);
    }
}
