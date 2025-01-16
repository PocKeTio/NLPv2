using Microsoft.Extensions.Options;
using NLPv2.Common;
using NLPv2.Models;

namespace NLPv2.Services
{
    public interface ICombinedClassificationService
    {
        ClassificationResult ClassifyText(string text);
        void LearnWeights(List<SwiftData> trainingData);
    }

    public class CombinedClassificationService : ICombinedClassificationService
    {
        private readonly IStatisticalClassificationService _statisticalService;
        private readonly IMLClassificationService _mlService;
        private readonly ILanguageDetectionService _languageDetectionService;
        private Dictionary<int, float> _categoryWeights = new();
        private float _alpha = 0.5f; // Poids relatif entre ML (alpha) et statistique (1-alpha)

        public CombinedClassificationService(
            IStatisticalClassificationService statisticalService,
            IMLClassificationService mlService,
            ILanguageDetectionService languageDetectionService)
        {
            _statisticalService = statisticalService;
            _mlService = mlService;
            _languageDetectionService = languageDetectionService;
        }

        public void LearnWeights(List<SwiftData> trainingData)
        {
            // 1. Diviser les données en ensembles d'entraînement et de validation
            var splitIndex = (int)(trainingData.Count * 0.8);
            var trainSet = trainingData.Take(splitIndex).ToList();
            var validSet = trainingData.Skip(splitIndex).ToList();

            // 2. Entraîner les modèles individuels
            _mlService.TrainModel(trainSet);
            _statisticalService.LearnPatterns(trainSet);

            // 3. Trouver le meilleur alpha global
            _alpha = FindOptimalAlpha(validSet);
            Console.WriteLine($"Alpha optimal trouvé : {_alpha:F3}");

            // 4. Calculer les poids par catégorie
            _categoryWeights = CalculateCategoryWeights(validSet);
            foreach (var (category, weight) in _categoryWeights.OrderBy(kv => kv.Key))
            {
                Console.WriteLine($"Poids pour catégorie {category}: {weight:F3}");
            }
        }

        private float FindOptimalAlpha(List<SwiftData> validationData)
        {
            float bestAlpha = 0.5f;
            float bestAccuracy = 0;

            // Tester différentes valeurs d'alpha
            for (float alpha = 0; alpha <= 1; alpha += 0.1f)
            {
                int correctPredictions = 0;

                foreach (var data in validationData)
                {
                    var result = CombinePredictions(
                        _mlService.ClassifyText(data.SWIFT),
                        _statisticalService.ClassifyText(data.SWIFT),
                        alpha);

                    if (result.Category == data.Category)
                    {
                        correctPredictions++;
                    }
                }

                float accuracy = (float)correctPredictions / validationData.Count;
                if (accuracy > bestAccuracy)
                {
                    bestAccuracy = accuracy;
                    bestAlpha = alpha;
                }
            }

            return bestAlpha;
        }

        private Dictionary<int, float> CalculateCategoryWeights(List<SwiftData> validationData)
        {
            var weights = new Dictionary<int, float>();
            var categories = validationData.Select(d => d.Category).Distinct();

            foreach (var category in categories)
            {
                var categoryData = validationData.Where(d => d.Category == category).ToList();
                
                int mlCorrect = 0;
                int statCorrect = 0;

                foreach (var data in categoryData)
                {
                    var mlResult = _mlService.ClassifyText(data.SWIFT);
                    var statResult = _statisticalService.ClassifyText(data.SWIFT);

                    if (mlResult.Category == category) mlCorrect++;
                    if (statResult.Category == category) statCorrect++;
                }

                float mlAccuracy = (float)mlCorrect / categoryData.Count;
                float statAccuracy = (float)statCorrect / categoryData.Count;

                // Le poids est la proportion de la précision ML par rapport à la somme des précisions
                weights[category] = mlAccuracy / (mlAccuracy + statAccuracy);
            }

            return weights;
        }

        public ClassificationResult ClassifyText(string text)
        {
            var language = _languageDetectionService.DetectLanguage(text);
            var mlResult = _mlService.ClassifyText(text);
            var statResult = _statisticalService.ClassifyText(text);

            return CombinePredictions(mlResult, statResult, _alpha);
        }

        private ClassificationResult CombinePredictions(
            ClassificationResult mlResult,
            ClassificationResult statResult,
            float alpha)
        {
            var combinedProbs = new Dictionary<int, float>();
            var categories = mlResult.Probabilities.Keys.Union(statResult.Probabilities.Keys);

            foreach (var category in categories)
            {
                mlResult.Probabilities.TryGetValue(category, out float mlProb);
                statResult.Probabilities.TryGetValue(category, out float statProb);

                // Utiliser le poids spécifique à la catégorie s'il existe, sinon utiliser alpha
                float categoryAlpha = _categoryWeights.TryGetValue(category, out float weight) ? weight : alpha;

                combinedProbs[category] = categoryAlpha * mlProb + (1 - categoryAlpha) * statProb;
            }

            return new ClassificationResult
            {
                Category = combinedProbs.OrderByDescending(kv => kv.Value).First().Key,
                Language = mlResult.Language,
                Probabilities = combinedProbs
            };
        }
    }
}
