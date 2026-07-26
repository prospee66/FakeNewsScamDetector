using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services;

// Scores a single URL for scam/phishing risk using a mix of cheap
// structural checks (done first, no network calls) and two external checks
// (WHOIS domain age, Google Safe Browsing). Each check that fires adds to
// a running risk total, capped at 1.0.
public class UrlAnalyzerService : IUrlAnalyzerService
{
    // Cheap top-level domains that legitimate sites rarely use, but scam
    // sites favor because they're cheap/easy to register in bulk.
    private static readonly string[] SuspiciousTlds = [".xyz", ".top", ".click", ".loan", ".gq", ".tk", ".ml"];

    // Common URL shorteners - not inherently malicious, but they hide the
    // real destination, which is itself a mild risk signal.
    private static readonly string[] UrlShorteners = ["bit.ly", "tinyurl.com", "t.co", "goo.gl", "is.gd"];

    private readonly IWhoisLookupClient _whoisClient;
    private readonly ISafeBrowsingClient _safeBrowsingClient;

    public UrlAnalyzerService(IWhoisLookupClient whoisClient, ISafeBrowsingClient safeBrowsingClient)
    {
        _whoisClient = whoisClient;
        _safeBrowsingClient = safeBrowsingClient;
    }

    public async Task<(double Score, List<string> Reasons)> AnalyzeUrlRiskAsync(string url)
    {
        // Can't parse it as a URL at all - treat as moderately risky rather
        // than 0 (clean) or 1 (certainly bad), since a malformed link is
        // suspicious but not proof of anything.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (0.5, []);

        double risk = 0;
        var reasons = new List<string>();

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            risk += 0.15;
            reasons.Add("Link does not use HTTPS");
        }

        if (System.Net.IPAddress.TryParse(uri.Host, out _))
        {
            risk += 0.30;
            reasons.Add("Link uses a raw IP address instead of a domain name");
        }

        if (SuspiciousTlds.Any(tld => uri.Host.EndsWith(tld, StringComparison.OrdinalIgnoreCase)))
        {
            risk += 0.25;
            reasons.Add("Domain uses a TLD commonly associated with scam sites");
        }

        if (UrlShorteners.Any(s => uri.Host.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            risk += 0.20;
            reasons.Add("Link uses a URL shortener, which hides the real destination");
        }

        if (uri.Host.Split('.').Length > 4)
        {
            risk += 0.15;
            reasons.Add("Domain has an unusually deep subdomain chain");
        }

        if (url.Contains('@'))
        {
            risk += 0.20;
            reasons.Add("Link contains an '@' character, a common URL-spoofing trick");
        }

        var hyphenCount = uri.Host.Count(c => c == '-');
        if (hyphenCount >= 3)
        {
            risk += 0.10;
            reasons.Add("Domain name contains an unusually high number of hyphens");
        }

        // WHOIS (raw TCP) and Safe Browsing (HTTP) don't depend on each other -
        // run them side by side instead of one after the other.
        var domainAgeTask = _whoisClient.GetDomainAgeInDaysAsync(uri.Host);
        var safeBrowsingTask = _safeBrowsingClient.CheckUrlAsync(url);
        await Task.WhenAll(domainAgeTask, safeBrowsingTask);

        var domainAgeDays = domainAgeTask.Result;
        if (domainAgeDays is not null)
        {
            if (domainAgeDays < 30)
            {
                risk += 0.35;
                reasons.Add($"Domain was registered only {domainAgeDays} day(s) ago — freshly registered domains are frequently used for scams");
            }
            else if (domainAgeDays < 180)
            {
                risk += 0.15;
                reasons.Add($"Domain was registered {domainAgeDays} days ago, relatively recently");
            }
        }

        var safeBrowsingResult = safeBrowsingTask.Result;
        if (safeBrowsingResult.IsFlagged)
        {
            risk += 0.60;
            reasons.Add($"Google Safe Browsing has flagged this URL ({string.Join(", ", safeBrowsingResult.ThreatTypes)})");
        }

        return (Math.Min(risk, 1.0), reasons);
    }
}
