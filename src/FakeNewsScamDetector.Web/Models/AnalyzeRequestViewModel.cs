using System.ComponentModel.DataAnnotations;

namespace FakeNewsScamDetector.Web.Models;

// Bound from the "Analyze Text or URL" form (see Analysis/Index.cshtml).
// The two attributes below are enforced both client-side (via the
// unobtrusive validation scripts) and server-side (ModelState.IsValid in
// AnalysisController), so a user can't bypass validation just by disabling
// JavaScript.
public class AnalyzeRequestViewModel
{
    [Required(ErrorMessage = "Please paste some text, a message, or a URL to analyze.")]
    [MinLength(5, ErrorMessage = "Please provide at least 5 characters.")]
    public string InputText { get; set; } = string.Empty;
}
