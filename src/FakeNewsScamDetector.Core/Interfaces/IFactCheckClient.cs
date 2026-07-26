using FakeNewsScamDetector.Core.Entities;

namespace FakeNewsScamDetector.Core.Interfaces;

// Looks up whether a claim has already been reviewed by a real fact-checker
// (Snopes, PolitiFact, etc.) via Google's Fact Check Tools API. Implemented
// by FactCheckClient.
public interface IFactCheckClient
{
    // Returns any matching published fact-checks for the given text. An
    // empty list just means nobody has reviewed this specific claim - not
    // that it's true.
    Task<List<FactCheckFinding>> SearchClaimsAsync(string text, CancellationToken cancellationToken = default);
}
