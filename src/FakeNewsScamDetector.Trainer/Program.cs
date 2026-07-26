using FakeNewsScamDetector.ML.Training;

// Standalone console app for (re)training both ML models from their CSV
// datasets. Not part of the web app's runtime - run this manually
// (`dotnet run --project src/FakeNewsScamDetector.Trainer`) whenever the
// datasets change or you want to try a different training pipeline. Output
// goes into src/FakeNewsScamDetector.ML/TrainedModels/, which the Web
// project copies into its own build output (see the Content item in
// FakeNewsScamDetector.Web.csproj).
var datasetsDir = Path.Combine(AppContext.BaseDirectory, "Datasets");
var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FakeNewsScamDetector.ML", "TrainedModels");
Directory.CreateDirectory(outputDir);

var fakeNewsDataset = Path.Combine(datasetsDir, "fake_news_dataset.csv");
var scamDataset = Path.Combine(datasetsDir, "scam_messages_dataset.csv");

if (File.Exists(fakeNewsDataset))
{
    Console.WriteLine("Training fake news model...");
    new FakeNewsModelTrainer().TrainAndSave(fakeNewsDataset, Path.Combine(outputDir, "fakeNewsModel.zip"));
}
else
{
    Console.WriteLine($"Skipping fake news model: dataset not found at {fakeNewsDataset}");
}

if (File.Exists(scamDataset))
{
    Console.WriteLine("Training scam text model...");
    new ScamTextModelTrainer().TrainAndSave(scamDataset, Path.Combine(outputDir, "scamTextModel.zip"));
}
else
{
    Console.WriteLine($"Skipping scam text model: dataset not found at {scamDataset}");
}

Console.WriteLine("Done.");
