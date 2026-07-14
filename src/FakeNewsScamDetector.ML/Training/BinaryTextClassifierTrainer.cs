using Microsoft.ML;
using Microsoft.ML.Data;

namespace FakeNewsScamDetector.ML.Training;

public record TrainingResult(
    ITransformer Model,
    DataViewSchema Schema,
    string AlgorithmName,
    BinaryClassificationMetrics Metrics);

// Both the fake-news and scam classifiers go through this same pipeline:
// try a few different trainers with cross-validation, keep whichever gets
// the best average AUC, then refit it on the full train split and score it
// against the held-out test split.
public static class BinaryTextClassifierTrainer
{
    public static TrainingResult TrainBest(
        MLContext mlContext,
        IDataView data,
        string textColumnName,
        string labelColumnName)
    {
        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 0);

        var featurization = mlContext.Transforms.Text
            .FeaturizeText("Features", textColumnName)
            .Append(mlContext.Transforms.CopyColumns("Label", labelColumnName));

        (string Name, IEstimator<ITransformer> Trainer)[] candidates =
        [
            ("FastTree", mlContext.BinaryClassification.Trainers.FastTree()),
            ("LightGbm", mlContext.BinaryClassification.Trainers.LightGbm()),
            ("SdcaLogisticRegression", mlContext.BinaryClassification.Trainers.SdcaLogisticRegression()),
        ];

        string bestName = candidates[0].Name;
        IEstimator<ITransformer> bestTrainer = candidates[0].Trainer;
        double bestAuc = double.NegativeInfinity;

        foreach (var (name, trainer) in candidates)
        {
            var pipeline = featurization.Append(trainer);
            var cvResults = mlContext.BinaryClassification.CrossValidate(
                split.TrainSet, pipeline, numberOfFolds: 5, labelColumnName: "Label");

            var avgAuc = cvResults.Average(r => r.Metrics.AreaUnderRocCurve);
            Console.WriteLine($"    {name,-24} CV AUC = {avgAuc:P2}");

            if (avgAuc > bestAuc)
            {
                bestAuc = avgAuc;
                bestName = name;
                bestTrainer = trainer;
            }
        }

        Console.WriteLine($"    Selected: {bestName}");

        var bestPipeline = featurization.Append(bestTrainer);
        var finalModel = bestPipeline.Fit(split.TrainSet);
        var testPredictions = finalModel.Transform(split.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(testPredictions, labelColumnName: "Label");

        return new TrainingResult(finalModel, data.Schema, bestName, metrics);
    }
}
