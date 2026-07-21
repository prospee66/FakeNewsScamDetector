using FakeNewsScamDetector.Core.Interfaces;
using FakeNewsScamDetector.ML.Models;
using Microsoft.ML;

namespace FakeNewsScamDetector.ML.Prediction;

public class TextClassifierService : ITextClassifierService
{
    private readonly MLContext _mlContext = new();
    private readonly PredictionEngine<NewsInputData, PredictionOutput>? _fakeNewsEngine;
    private readonly PredictionEngine<ScamInputData, PredictionOutput>? _scamEngine;

    // PredictionEngine isn't thread-safe (see ML.NET docs), but this service is
    // registered as a singleton so requests from different users hit the same
    // engine instance concurrently. A lock per engine keeps predictions correct
    // under load; the alternative (PredictionEnginePool) would need a new package.
    private readonly object _fakeNewsLock = new();
    private readonly object _scamLock = new();

    public TextClassifierService(string fakeNewsModelPath, string scamModelPath)
    {
        if (File.Exists(fakeNewsModelPath))
        {
            var fakeNewsModel = _mlContext.Model.Load(fakeNewsModelPath, out _);
            _fakeNewsEngine = _mlContext.Model.CreatePredictionEngine<NewsInputData, PredictionOutput>(fakeNewsModel);
        }

        if (File.Exists(scamModelPath))
        {
            var scamModel = _mlContext.Model.Load(scamModelPath, out _);
            _scamEngine = _mlContext.Model.CreatePredictionEngine<ScamInputData, PredictionOutput>(scamModel);
        }
    }

    public Task<double> PredictFakeNewsScoreAsync(string text)
    {
        if (_fakeNewsEngine is null)
            return Task.FromResult(0.5);

        lock (_fakeNewsLock)
        {
            var result = _fakeNewsEngine.Predict(new NewsInputData { Text = text });
            return Task.FromResult((double)result.Probability);
        }
    }

    public Task<double> PredictScamScoreAsync(string text)
    {
        if (_scamEngine is null)
            return Task.FromResult(0.5);

        lock (_scamLock)
        {
            var result = _scamEngine.Predict(new ScamInputData { Text = text });
            return Task.FromResult((double)result.Probability);
        }
    }
}
