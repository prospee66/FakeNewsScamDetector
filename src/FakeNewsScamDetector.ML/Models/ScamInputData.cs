using Microsoft.ML.Data;

namespace FakeNewsScamDetector.ML.Models;

// Row shape for the scam-text training CSV
// (Datasets/scam_messages_dataset.csv: "Text,IsScam") and for a single
// message passed in for prediction. LoadColumn indexes map to CSV column
// position, not name.
public class ScamInputData
{
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    // True if this text is a known scam example (only meaningful during
    // training - left default/unused when used for prediction).
    [LoadColumn(1)]
    public bool IsScam { get; set; }
}
