using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using FakeNewsScamDetector.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FakeNewsScamDetector.Services.ExternalApis;

// Gets a domain's registration date the old-fashioned way: raw WHOIS over
// TCP port 43 (RFC 3912), no API key needed. First asks IANA which registry
// is authoritative for the TLD, then queries that registry directly and
// pulls the creation date out of whatever free-text format it replies with.
// Registries don't all format this the same way, so anything we can't parse
// (or any network hiccup) just comes back as null - domain age becomes an
// unknown signal rather than blowing up the request.
public class WhoisLookupClient : IWhoisLookupClient
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(4);
    // Two round-trips over raw TCP make this the slowest step in a scan, and
    // a domain's registration date never changes, so caching it avoids paying
    // that cost again for every scan of the same domain.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private static readonly Regex[] CreationDatePatterns =
    [
        new(@"(?:Creation Date|created|Registered on|Domain Registration Date|Registration Time|created-date)\s*:\s*(.+)", RegexOptions.IgnoreCase),
    ];

    private readonly IMemoryCache _cache;

    public WhoisLookupClient(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<int?> GetDomainAgeInDaysAsync(string domain, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"whois:{domain.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out int? cachedAge))
            return cachedAge;

        var age = await LookupDomainAgeInDaysAsync(domain, cancellationToken);
        _cache.Set(cacheKey, age, CacheDuration);
        return age;
    }

    private static async Task<int?> LookupDomainAgeInDaysAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var tld = ExtractTld(domain);
            if (tld is null)
                return null;

            var referralServer = await FindAuthoritativeServerAsync(tld, cancellationToken) ?? "whois.iana.org";
            var record = await QueryWhoisServerAsync(referralServer, domain, cancellationToken);
            if (record is null)
                return null;

            var createdAt = ParseCreationDate(record);
            if (createdAt is null)
                return null;

            return Math.Max(0, (int)(DateTime.UtcNow - createdAt.Value).TotalDays);
        }
        catch
        {
            // Any network failure, malformed response, or unparseable date is a
            // "we don't know" outcome, not an error worth surfacing to the user.
            return null;
        }
    }

    private static string? ExtractTld(string domain)
    {
        var parts = domain.Trim().TrimEnd('.').Split('.');
        return parts.Length >= 2 ? parts[^1].ToLowerInvariant() : null;
    }

    private static async Task<string?> FindAuthoritativeServerAsync(string tld, CancellationToken cancellationToken)
    {
        var response = await QueryWhoisServerAsync("whois.iana.org", tld, cancellationToken);
        if (response is null)
            return null;

        var match = Regex.Match(response, @"^whois:\s*(\S+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task<string?> QueryWhoisServerAsync(string server, string query, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(QueryTimeout);

        using var client = new TcpClient();
        await client.ConnectAsync(server, 43, cts.Token);

        using var stream = client.GetStream();
        var queryBytes = Encoding.ASCII.GetBytes(query + "\r\n");
        await stream.WriteAsync(queryBytes, cts.Token);

        using var reader = new StreamReader(stream, Encoding.ASCII);
        var response = await reader.ReadToEndAsync(cts.Token);
        return string.IsNullOrWhiteSpace(response) ? null : response;
    }

    private static DateTime? ParseCreationDate(string whoisRecord)
    {
        foreach (var pattern in CreationDatePatterns)
        {
            var match = pattern.Match(whoisRecord);
            if (!match.Success)
                continue;

            var rawValue = match.Groups[1].Value.Trim();
            if (DateTime.TryParse(rawValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return parsed.ToUniversalTime();
        }

        return null;
    }
}
