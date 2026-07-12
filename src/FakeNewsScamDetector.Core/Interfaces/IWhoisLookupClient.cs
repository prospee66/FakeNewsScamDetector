namespace FakeNewsScamDetector.Core.Interfaces;

public interface IWhoisLookupClient
{
    Task<int?> GetDomainAgeInDaysAsync(string domain, CancellationToken cancellationToken = default);
}
