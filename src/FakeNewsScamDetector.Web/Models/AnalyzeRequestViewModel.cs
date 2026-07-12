using System.ComponentModel.DataAnnotations;

namespace FakeNewsScamDetector.Web.Models;

public class AnalyzeRequestViewModel
{
    [Required(ErrorMessage = "Please paste some text, a message, or a URL to analyze.")]
    [MinLength(5, ErrorMessage = "Please provide at least 5 characters.")]
    public string InputText { get; set; } = string.Empty;
}
