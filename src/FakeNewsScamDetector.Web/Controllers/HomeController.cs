using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FakeNewsScamDetector.Web.Models;

namespace FakeNewsScamDetector.Web.Controllers;

// Landing page, privacy page, and the generic error page. No dependencies -
// these are just static content, unlike every other controller here.
public class HomeController : Controller
{
    // The marketing/landing page shown at "/".
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // Wired up as the fallback in Program.cs's UseExceptionHandler for any
    // unhandled exception in Production. Never cached, since showing a
    // stale error page (or caching a transient one) would be confusing.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Activity.Current is ASP.NET Core's distributed-tracing id if one
        // is active; TraceIdentifier is the built-in per-request id
        // fallback. Either way, this gives the user something to quote if
        // they report the error.
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
