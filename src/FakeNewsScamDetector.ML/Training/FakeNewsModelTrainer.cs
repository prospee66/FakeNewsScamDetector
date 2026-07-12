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

        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        var pipeline = _mlContext.Transforms.Text
            .FeaturizeText("Features", nameof(NewsInputData.Text))
            .Append(_mlContext.Transforms.CopyColumns("Label", nameof(NewsInputData.IsFake)))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree());

        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions);
        Console.WriteLine($"Fake News Model — Accuracy: {metrics.Accuracy:P2}, F1: {metrics.F1Score:P2}");

        _mlContext.Model.Save(model, data.Schema, modelOutputPath);
    }
}
