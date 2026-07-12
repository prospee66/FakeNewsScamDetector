using FakeNewsScamDetector.Core.Entities;

namespace FakeNewsScamDetector.Core.Interfaces;

public interface IFactCheckClient
{
    Task<List<FactCheckFinding>> SearchClaimsAsync(string text, CancellationToken cancellationToken = default);
}
