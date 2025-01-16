using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLPv2.Models;

namespace NLPv2.Services
{
    public interface IStatisticalClassificationService
    {
        void LearnPatterns(List<SwiftData> trainingData);
        ClassificationResult ClassifyText(string text);
        Dictionary<int, List<(string pattern, float weight)>> GetLearnedPatterns();
    }

    public class StatisticalClassificationService : IStatisticalClassificationService
    {
        private readonly ILanguageDetectionService _languageDetectionService;
        private Dictionary<int, Dictionary<string, float>> _patternWeights;
        private const int MinOccurrences = 3;  // Nombre minimum d'occurrences pour considérer un pattern
        private const float MinCorrelation = 0.1f;  // Corrélation minimale pour garder un pattern

        public StatisticalClassificationService(ILanguageDetectionService languageDetectionService)
        {
            _languageDetectionService = languageDetectionService;
            _patternWeights = new Dictionary<int, Dictionary<string, float>>();
        }

        public void LearnPatterns(List<SwiftData> trainingData)
        {
            // 1. Extraire tous les mots et expressions potentiels
            var allPatterns = ExtractPotentialPatterns(trainingData);

            // 2. Calculer les corrélations pour chaque pattern et catégorie
            _patternWeights = new Dictionary<int, Dictionary<string, float>>();
            var categories = trainingData.Select(d => d.Category).Distinct().OrderBy(c => c);

            foreach (var category in categories)
            {
                var categoryPatterns = new Dictionary<string, float>();
                var categoryData = trainingData.Where(d => d.Category == category).ToList();
                var otherData = trainingData.Where(d => d.Category != category).ToList();

                foreach (var pattern in allPatterns)
                {
                    // Calculer la corrélation point-bisériale entre le pattern et la catégorie
                    float correlation = CalculateCorrelation(pattern, categoryData, otherData);

                    if (Math.Abs(correlation) >= MinCorrelation)
                    {
                        categoryPatterns[pattern] = correlation;
                    }
                }

                if (categoryPatterns.Any())
                {
                    _patternWeights[category] = categoryPatterns;
                }
            }

            // Afficher les patterns appris pour chaque catégorie
            foreach (var category in _patternWeights.Keys)
            {
                Console.WriteLine($"\nPatterns appris pour la catégorie {category}:");
                var sortedPatterns = _patternWeights[category]
                    .OrderByDescending(x => Math.Abs(x.Value))
                    .Take(10);

                foreach (var (pattern, weight) in sortedPatterns)
                {
                    Console.WriteLine($"- '{pattern}': {weight:F3}");
                }
            }
        }

        public ClassificationResult ClassifyText(string text)
        {
            if (_patternWeights == null || !_patternWeights.Any())
            {
                throw new InvalidOperationException("Les patterns doivent être appris avant de classifier");
            }

            text = text.ToLower();
            var scores = new Dictionary<int, float>();

            // Calculer le score pour chaque catégorie
            foreach (var category in _patternWeights.Keys)
            {
                float score = 0;
                var patterns = _patternWeights[category];

                foreach (var (pattern, weight) in patterns)
                {
                    int occurrences = CountOccurrences(text, pattern);
                    if (occurrences > 0)
                    {
                        score += weight * occurrences;
                    }
                }

                scores[category] = score;
            }

            // Normaliser les scores avec softmax
            var distribution = new Dictionary<int, float>();
            float maxScore = scores.Values.Max();
            float sumExp = scores.Values.Sum(s => (float)Math.Exp(s - maxScore));

            foreach (var category in scores.Keys)
            {
                distribution[category] = (float)Math.Exp(scores[category] - maxScore) / sumExp;
            }

            return new ClassificationResult
            {
                Category = distribution.OrderByDescending(x => x.Value).First().Key,
                Language = _languageDetectionService.DetectLanguage(text),
                Probabilities = distribution
            };
        }

        public Dictionary<int, List<(string pattern, float weight)>> GetLearnedPatterns()
        {
            return _patternWeights.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(p => (p.Key, p.Value)).ToList()
            );
        }

        private HashSet<string> ExtractPotentialPatterns(List<SwiftData> data)
        {
            var patterns = new HashSet<string>();
            var wordCounts = new Dictionary<string, int>();

            // 1. Extraire et compter tous les mots et bi-grammes
            foreach (var item in data)
            {
                var words = TokenizeText(item.SWIFT);
                
                // Ajouter les mots individuels
                foreach (var word in words)
                {
                    if (word.Length >= 3) // Ignorer les mots trop courts
                    {
                        wordCounts.TryGetValue(word, out int count);
                        wordCounts[word] = count + 1;
                    }
                }

                // Ajouter les bi-grammes
                for (int i = 0; i < words.Count - 1; i++)
                {
                    if (words[i].Length >= 3 && words[i + 1].Length >= 3)
                    {
                        string bigram = $"{words[i]} {words[i + 1]}";
                        wordCounts.TryGetValue(bigram, out int count);
                        wordCounts[bigram] = count + 1;
                    }
                }
            }

            // 2. Ne garder que les patterns qui apparaissent suffisamment
            foreach (var kvp in wordCounts)
            {
                if (kvp.Value >= MinOccurrences)
                {
                    patterns.Add(kvp.Key);
                }
            }

            return patterns;
        }

        private float CalculateCorrelation(string pattern, List<SwiftData> categoryData, List<SwiftData> otherData)
        {
            int totalDocs = categoryData.Count + otherData.Count;
            
            // Compter les occurrences dans la catégorie
            float categoryPresence = categoryData.Count(d => ContainsPattern(d.SWIFT, pattern));
            float categoryAbsence = categoryData.Count - categoryPresence;
            
            // Compter les occurrences dans les autres catégories
            float otherPresence = otherData.Count(d => ContainsPattern(d.SWIFT, pattern));
            float otherAbsence = otherData.Count - otherPresence;

            // Calculer le coefficient de corrélation phi (version simplifiée de point-bisériale)
            float numerator = (categoryPresence * otherAbsence) - (categoryAbsence * otherPresence);
            float denominator = (float)Math.Sqrt(
                (categoryPresence + categoryAbsence) *
                (otherPresence + otherAbsence) *
                (categoryPresence + otherPresence) *
                (categoryAbsence + otherAbsence)
            );

            if (denominator == 0) return 0;
            return numerator / denominator;
        }

        private List<string> TokenizeText(string text)
        {
            text = text.ToLower();
            // Remplacer la ponctuation par des espaces
            text = Regex.Replace(text, @"[^\w\s]", " ");
            // Diviser en mots et retirer les espaces superflus
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private bool ContainsPattern(string text, string pattern)
        {
            return text.ToLower().Contains(pattern);
        }

        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            text = text.ToLower();
            pattern = pattern.ToLower();

            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }
    }
}
