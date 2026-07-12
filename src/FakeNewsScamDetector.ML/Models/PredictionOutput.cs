using Microsoft.ML.Data;

namespace FakeNewsScamDetector.ML.Models;

public class PredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
