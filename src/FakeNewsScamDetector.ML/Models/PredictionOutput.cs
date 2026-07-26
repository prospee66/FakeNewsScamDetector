using Microsoft.ML.Data;

namespace FakeNewsScamDetector.ML.Models;

// Shared output shape for both binary classifiers (fake-news and scam-text)
// - ML.NET fills these properties in by matching names/attributes against
// whatever the trained model pipeline produces.
public class PredictionOutput
{
    // The model's binary yes/no call at its default 0.5 probability
    // threshold. Not actually used by TextClassifierService - it reads
    // Probability instead, since the app blends scores together rather
    // than treating any single signal as a hard yes/no.
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    // Calibrated 0-1 probability of the positive class (IsFake/IsScam).
    // This is the value TextClassifierService actually returns.
    public float Probability { get; set; }

    // Raw, uncalibrated model score (e.g. log-odds). Not currently used -
    // Probability is the one that's meaningful as a percentage.
    public float Score { get; set; }
}
