# FakeNewsScamDetector

ASP.NET Core MVC app that scores pasted text/URLs for scam and fake-news risk, combining an ML.NET classifier with rule-based heuristics and URL analysis.

## Projects

- `FakeNewsScamDetector.Core` — domain entities, enums, interfaces
- `FakeNewsScamDetector.ML` — ML.NET training and prediction
- `FakeNewsScamDetector.Services` — rule engine, URL analyzer, verdict aggregator
- `FakeNewsScamDetector.Data` — EF Core (SQLite) persistence
- `FakeNewsScamDetector.Web` — MVC front end (composition root)
- `FakeNewsScamDetector.Trainer` — console app to train and export models
- `FakeNewsScamDetector.Tests` — xUnit tests

## Running

```bash
dotnet build
dotnet run --project src/FakeNewsScamDetector.Web
```

The app creates a local `app.db` SQLite database on first run.

## Training models

Sample starter datasets live in `src/FakeNewsScamDetector.Trainer/Datasets/` (tiny, for demo purposes only — replace with real data before relying on model accuracy). To train:

```bash
dotnet run --project src/FakeNewsScamDetector.Trainer
```

This writes `fakeNewsModel.zip` and `scamTextModel.zip` to `src/FakeNewsScamDetector.ML/TrainedModels/`, which the Web project picks up automatically on its next build. Without trained models present, `TextClassifierService` falls back to a neutral 0.5 score so the app still runs.

## Tests

```bash
dotnet test
```
