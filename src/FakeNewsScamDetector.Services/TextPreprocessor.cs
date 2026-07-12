using System.Text.RegularExpressions;

namespace FakeNewsScamDetector.Services;

public static class TextPreprocessor
{
    private static readonly Regex UrlPattern = new(
        @"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Normalize(string text)
    {
        return text.Trim().Replace("\r\n", "\n");
    }

    public static string? ExtractFirstUrl(string text)
    {
        var match = UrlPattern.Match(text);
        return match.Success ? match.Value : null;
    }

    public static string StripUrls(string text)
    {
        return UrlPattern.Replace(text, string.Empty).Trim();
    }
}
