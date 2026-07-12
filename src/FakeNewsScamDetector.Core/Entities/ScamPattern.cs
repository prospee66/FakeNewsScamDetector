namespace FakeNewsScamDetector.Core.Entities;

public class ScamPattern
{
    public int Id { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double WeightScore { get; set; }
}
