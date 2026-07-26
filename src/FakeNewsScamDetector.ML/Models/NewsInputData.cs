using Microsoft.ML.Data;

namespace FakeNewsScamDetector.ML.Models;

// Row shape for the fake-news training CSV (Datasets/fake_news_dataset.csv:
// "Text,IsFake") and for a single piece of text passed in for prediction.
// LoadColumn indexes map to CSV column position, not name.
public class NewsInputData
{
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    // True if this text is a known fake-news example (only meaningful
    // during training - left default/unused when used for prediction).
    [LoadColumn(1)]
    public bool IsFake { get; set; }
}
