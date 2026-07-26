using FakeNewsScamDetector.ML.Models;
using Microsoft.ML;

namespace FakeNewsScamDetector.ML.Training;

// Entry point for training the scam-text classifier specifically. Mirrors
// FakeNewsModelTrainer, just pointed at the scam dataset/columns. Invoked
// by FakeNewsScamDetector.Trainer's Program.cs.
public class ScamTextModelTrainer
{
    // seed: 0 makes the train/test split and any randomized trainer
    // internals reproducible between runs.
    private readonly MLContext _mlContext = new(seed: 0);

    public void TrainAndSave(string datasetPath, string modelOutputPath)
    {
        IDataView data = _mlContext.Data.LoadFromTextFile<ScamInputData>(
            datasetPath, hasHeader: true, separatorChar: ',', allowQuoting: true);

        var result = BinaryTextClassifierTrainer.TrainBest(
            _mlContext, data, nameof(ScamInputData.Text), nameof(ScamInputData.IsScam));

        var report = ModelMetricsReport.From(result.AlgorithmName, result.Metrics);
        Console.WriteLine("  Scam Text Model:");
        report.Print();

        // Regression guard - see the matching comment in
        // FakeNewsModelTrainer for why this check exists.
        var previous = ModelMetricsReport.Load(modelOutputPath);
        if (previous is not null && previous.Auc >= report.Auc)
        {
            Console.WriteLine($"    Skipped save: new AUC ({report.Auc:P2}) does not beat existing model ({previous.Auc:P2}).");
            return;
        }

        _mlContext.Model.Save(result.Model, result.Schema, modelOutputPath);
        report.Save(modelOutputPath);
        Console.WriteLine($"    Saved to {modelOutputPath}");
    }
}
