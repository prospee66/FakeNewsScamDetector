using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services;

public class UrlAnalyzerService : IUrlAnalyzerService
{
    private static readonly string[] SuspiciousTlds = [".xyz", ".top", ".click", ".loan", ".gq", ".tk", ".ml"];
    private static readonly string[] UrlShorteners = ["bit.ly", "tinyurl.com", "t.co", "goo.gl", "is.gd"];

    public Task<double> AnalyzeUrlRiskAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return Task.FromResult(0.5);

        double risk = 0;

        if (uri.Scheme != Uri.UriSchemeHttps)
            risk += 0.15;

        if (System.Net.IPAddress.TryParse(uri.Host, out _))
            risk += 0.30;

        if (SuspiciousTlds.Any(tld => uri.Host.EndsWith(tld, StringComparison.OrdinalIgnoreCase)))
            risk += 0.25;

        if (UrlShorteners.Any(s => uri.Host.Contains(s, StringComparison.OrdinalIgnoreCase)))
            risk += 0.20;

        if (uri.Host.Split('.').Length > 4)
            risk += 0.15;

        if (url.Contains('@'))
            risk += 0.20;

        var hyphenCount = uri.Host.Count(c => c == '-');
        if (hyphenCount >= 3)
            risk += 0.10;

        return Task.FromResult(Math.Min(risk, 1.0));
    }
}
