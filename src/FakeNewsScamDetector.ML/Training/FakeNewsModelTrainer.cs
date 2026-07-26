using FakeNewsScamDetector.ML.Models;
using Microsoft.ML;

namespace FakeNewsScamDetector.ML.Training;

// Entry point for training the fake-news classifier specifically. Thin
// wrapper around BinaryTextClassifierTrainer that knows which dataset
// columns to use and handles printing/saving the result. Invoked by
// FakeNewsScamDetector.Trainer's Program.cs.
public class FakeNewsModelTrainer
{
    // seed: 0 makes the train/test split and any randomized trainer
    // internals reproducible between runs, so re-running training on an
    // unchanged dataset gives comparable metrics instead of noisy ones.
    private readonly MLContext _mlContext = new(seed: 0);

    public void TrainAndSave(string datasetPath, string modelOutputPath)
    {
        IDataView data = _mlContext.Data.LoadFromTextFile<NewsInputData>(
            datasetPath, hasHeader: true, separatorChar: ',', allowQuoting: true);

        var result = BinaryTextClassifierTrainer.TrainBest(
            _mlContext, data, nameof(NewsInputData.Text), nameof(NewsInputData.IsFake));

        var report = ModelMetricsReport.From(result.AlgorithmName, result.Metrics);
        Console.WriteLine("  Fake News Model:");
        report.Print();

        // Regression guard: only overwrite the model that's actually
        // shipped with the app if the freshly trained one is measurably
        // better. Without this, a bad dataset change or an unlucky
        // train/test split could silently make the live model worse.
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
