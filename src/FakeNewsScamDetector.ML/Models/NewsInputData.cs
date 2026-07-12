using Microsoft.ML.Data;

namespace FakeNewsScamDetector.ML.Models;

public class NewsInputData
{
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    [LoadColumn(1)]
    public bool IsFake { get; set; }
}
