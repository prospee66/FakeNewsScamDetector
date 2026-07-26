namespace FakeNewsScamDetector.Web.Models;

// Shown by HomeController.Error - just enough info for a user to reference
// when reporting a problem, without exposing exception details to them.
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
