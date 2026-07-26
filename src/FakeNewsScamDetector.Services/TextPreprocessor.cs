using System.Text.RegularExpressions;

namespace FakeNewsScamDetector.Services;

// Small text-cleanup helpers shared by VerdictAggregator before it hands
// text off to the ML classifiers, rule engine, and URL analyzer.
public static class TextPreprocessor
{
    // Compiled once and reused (RegexOptions.Compiled trades a slower first
    // use for faster matching afterward) since this runs on every scan.
    private static readonly Regex UrlPattern = new(
        @"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Trims whitespace and normalizes Windows-style line endings to \n, so
    // downstream regexes/keyword matches behave consistently regardless of
    // where the pasted text came from.
    public static string Normalize(string text)
    {
        return text.Trim().Replace("\r\n", "\n");
    }

    // Returns the first http(s) URL found in the text, or null if there
    // isn't one. Only the first URL is analyzed - see VerdictAggregator.
    public static string? ExtractFirstUrl(string text)
    {
        var match = UrlPattern.Match(text);
        return match.Success ? match.Value : null;
    }

    // Removes all URLs from the text, so the ML classifier and rule engine
    // are scoring the surrounding message content, not raw link text.
    public static string StripUrls(string text)
    {
        return UrlPattern.Replace(text, string.Empty).Trim();
    }
}
