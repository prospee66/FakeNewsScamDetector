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

`src/FakeNewsScamDetector.Trainer/Datasets/` holds two real public datasets, reshaped into `Text,IsFake`/`Text,IsScam` CSVs:

- `fake_news_dataset.csv` — derived from the [LIAR dataset](https://www.cs.ucsb.edu/~william/data/liar_dataset.zip) (~12.8k labeled political statements; the 6-way truthfulness scale is collapsed to binary).
- `scam_messages_dataset.csv` — the [SMS Spam Collection](https://archive.ics.uci.edu/dataset/228/sms+spam+collection) (~5.6k labeled messages; spam is used as a proxy for scam/phishing text).

To train:

```bash
dotnet run --project src/FakeNewsScamDetector.Trainer --configuration Release
```

For each dataset, the trainer 5-fold cross-validates three ML.NET algorithms (FastTree, LightGbm, SdcaLogisticRegression), picks the best by AUC, refits it, and evaluates on a held-out test split. A model is only saved over the existing one if its AUC is higher — metrics for the currently-saved model live alongside it as `TrainedModels/*.metrics.json`. Latest results:

| Model | Algorithm | Accuracy | AUC | F1 |
|---|---|---|---|---|
| Fake News | SdcaLogisticRegression | 61.7% | 66.0% | 56.7% |
| Scam Text | FastTree | 98.6% | 99.2% | 94.9% |

The fake-news number looks low next to the scam number because LIAR is a genuinely hard benchmark (short political claims judged without context) — published baselines on it are typically in the same 60-70% range. Scam/spam detection is a much easier signal.

Models are copied next to the Web project's build output on its next build. Without trained models present, `TextClassifierService` falls back to a neutral 0.5 score so the app still runs.

## Tests

```bash
dotnet test
```
