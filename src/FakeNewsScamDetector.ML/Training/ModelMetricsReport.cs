using System.Text.Json;
using Microsoft.ML.Data;

namespace FakeNewsScamDetector.ML.Training;

public record ModelMetricsReport(
    string AlgorithmName,
    double Accuracy,
    double Auc,
    double Auprc,
    double F1Score,
    double PositivePrecision,
    double PositiveRecall,
    DateTime TrainedAtUtc)
{
    public static ModelMetricsReport From(string algorithmName, BinaryClassificationMetrics metrics) => new(
        algorithmName,
        metrics.Accuracy,
        metrics.AreaUnderRocCurve,
        metrics.AreaUnderPrecisionRecallCurve,
        metrics.F1Score,
        metrics.PositivePrecision,
        metrics.PositiveRecall,
        DateTime.UtcNow);

    public void Print()
    {
        Console.WriteLine($"    Algorithm:          {AlgorithmName}");
        Console.WriteLine($"    Accuracy:           {Accuracy:P2}");
        Console.WriteLine($"    AUC:                {Auc:P2}");
        Console.WriteLine($"    AUPRC:              {Auprc:P2}");
        Console.WriteLine($"    F1 Score:           {F1Score:P2}");
        Console.WriteLine($"    Positive Precision: {PositivePrecision:P2}");
        Console.WriteLine($"    Positive Recall:    {PositiveRecall:P2}");
    }

    public static string MetricsPathFor(string modelPath) =>
        Path.ChangeExtension(modelPath, ".metrics.json");

    public static ModelMetricsReport? Load(string modelPath)
    {
        var path = MetricsPathFor(modelPath);
        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize<ModelMetricsReport>(File.ReadAllText(path));
    }

    public void Save(string modelPath)
    {
        File.WriteAllText(MetricsPathFor(modelPath), JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
