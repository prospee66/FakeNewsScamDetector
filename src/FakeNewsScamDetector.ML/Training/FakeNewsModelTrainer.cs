using FakeNewsScamDetector.ML.Models;
using Microsoft.ML;

namespace FakeNewsScamDetector.ML.Training;

public class FakeNewsModelTrainer
{
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
