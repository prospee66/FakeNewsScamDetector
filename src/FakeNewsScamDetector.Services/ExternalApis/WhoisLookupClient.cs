using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using FakeNewsScamDetector.Core.Interfaces;

namespace FakeNewsScamDetector.Services.ExternalApis;

/// <summary>
/// Looks up a domain's registration date via the raw WHOIS protocol (RFC 3912,
/// TCP port 43) — no API key required. Queries IANA's root WHOIS server to find
/// the authoritative registry for the domain's TLD, then queries that registry
/// directly and parses the creation date out of its free-text response.
/// WHOIS response formats vary by registry and aren't guaranteed to be parseable;
/// any failure (timeout, unknown format, unregistered domain) degrades to null so
/// callers can treat domain age as an unknown, neutral signal rather than erroring.
/// </summary>
public class WhoisLookupClient : IWhoisLookupClient
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(4);

    private static readonly Regex[] CreationDatePatterns =
    [
        new(@"(?:Creation Date|created|Registered on|Domain Registration Date|Registration Time|created-date)\s*:\s*(.+)", RegexOptions.IgnoreCase),
    ];

    public async Task<int?> GetDomainAgeInDaysAsync(string domain, CancellationToken cancellationToken = default)
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
