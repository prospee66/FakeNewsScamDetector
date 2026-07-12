namespace FakeNewsScamDetector.Core.Interfaces;

public interface IScamRuleEngine
{
    (double Score, List<string> MatchedReasons) EvaluateText(string text);
}
