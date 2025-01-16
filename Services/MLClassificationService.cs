using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using NLPv2.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NLPv2.Common;

namespace NLPv2.Services
{
    public interface IMLClassificationService
    {
        void TrainModel(List<SwiftData> trainingData);
        void EvaluateModel(List<SwiftData> testData);
        ClassificationResult ClassifyText(string text);
    }

    public class MLClassificationService : IMLClassificationService
    {
        private readonly MLContext _mlContext;
        private ITransformer _trainedModel;
        private PredictionEngine<SwiftData, SwiftPrediction> _predictionEngine;
        private readonly MLSettings _settings;
        private readonly ILanguageDetectionService _languageDetectionService;

        public MLClassificationService(IOptions<MLSettings> settings, ILanguageDetectionService languageDetectionService)
        {
            _mlContext = new MLContext(seed: 1);
            _settings = settings.Value;
            _languageDetectionService = languageDetectionService;
        }

        public void TrainModel(List<SwiftData> trainingData)
        {
            if (trainingData == null || !trainingData.Any())
                throw new ArgumentException("Training data cannot be empty", nameof(trainingData));

            // Create data view
            var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Define data preparation pipeline
            var pipeline = _mlContext.Transforms.Text
                .NormalizeText("SWIFT")
                .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "SWIFT"))
                .Append(_mlContext.Transforms.Text.RemoveDefaultStopWords("Tokens"))
                .Append(_mlContext.Transforms.Text.ProduceNgrams("Tokens"))
                .Append(_mlContext.Transforms.Text.FeaturizeText("Features", "Tokens"))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", "Category"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    maximumNumberOfIterations: 100))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // Train model
            _trainedModel = pipeline.Fit(trainingDataView);

            // Create prediction engine
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<SwiftData, SwiftPrediction>(_trainedModel);

            // Save model if path is specified
            if (!string.IsNullOrEmpty(_settings.SaveModelPath))
            {
                _mlContext.Model.Save(_trainedModel, trainingDataView.Schema, _settings.SaveModelPath);
            }
        }

        public void EvaluateModel(List<SwiftData> testData)
        {
            if (_trainedModel == null)
                throw new InvalidOperationException("Model must be trained before evaluation");

            var testDataView = _mlContext.Data.LoadFromEnumerable(testData);
            var predictions = _trainedModel.Transform(testDataView);

            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

            Console.WriteLine($"Macro Accuracy: {metrics.MacroAccuracy:P2}");
            Console.WriteLine($"Micro Accuracy: {metrics.MicroAccuracy:P2}");
            Console.WriteLine($"Log Loss: {metrics.LogLoss:F4}");
            Console.WriteLine("\nConfusion Matrix:");
            Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
        }

        public ClassificationResult ClassifyText(string text)
        {
            if (_trainedModel == null || _predictionEngine == null)
                throw new InvalidOperationException("Model must be trained before making predictions");

            var prediction = _predictionEngine.Predict(new SwiftData { SWIFT = text });

            var probabilities = new Dictionary<int, float>();
            for (int i = 0; i < prediction.Score.Length; i++)
            {
                probabilities[i] = prediction.Score[i];
            }

            return new ClassificationResult
            {
                Category = (int)prediction.PredictedLabel,
                Language = _languageDetectionService.DetectLanguage(text),
                Probabilities = probabilities
            };
        }
    }

    public class SwiftPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }

        [ColumnName("Score")]
        public float[] Score { get; set; }
    }
}
