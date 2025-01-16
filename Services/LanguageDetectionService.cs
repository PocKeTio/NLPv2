using System.Linq;
using System.Collections.Generic;

namespace NLPv2.Services
{
    public interface ILanguageDetectionService
    {
        int DetectLanguage(string text);
    }

    public class LanguageDetectionService : ILanguageDetectionService
    {
        private static readonly Dictionary<int, Dictionary<string, float>> _languagePatterns = new Dictionary<int, Dictionary<string, float>>
        {
            // Anglais (1)
            { 1, new Dictionary<string, float> {
                // Termes bancaires spécifiques
                {"payment", 2.0f}, {"account", 2.0f}, {"transfer", 2.0f}, {"amount", 2.0f},
                {"settlement", 2.0f}, {"transaction", 2.0f}, {"balance", 2.0f}, {"credit", 2.0f},
                {"debit", 2.0f}, {"interest", 2.0f}, {"maturity", 2.0f}, {"currency", 2.0f},
                {"exchange", 2.0f}, {"rate", 2.0f}, {"fee", 2.0f}, {"charge", 2.0f},
                {"swift", 2.0f}, {"bank", 2.0f}, {"branch", 2.0f}, {"beneficiary", 2.0f},
                {"remittance", 2.0f}, {"overdraft", 2.0f}, {"deposit", 2.0f},
                
                // Termes juridiques/contractuels
                {"agreement", 1.5f}, {"contract", 1.5f}, {"terms", 1.5f}, {"conditions", 1.5f},
                {"clause", 1.5f}, {"party", 1.5f}, {"hereby", 1.5f}, {"thereof", 1.5f},
                {"pursuant", 1.5f}, {"provision", 1.5f},
                
                // Mots courants mais importants
                {"the", 0.5f}, {"and", 0.5f}, {"of", 0.5f}, {"to", 0.5f}, {"in", 0.5f},
                {"for", 0.5f}, {"with", 0.5f}, {"by", 0.5f}, {"on", 0.5f}, {"at", 0.5f}
            }},
            
            // Français (2)
            { 2, new Dictionary<string, float> {
                // Termes bancaires spécifiques
                {"paiement", 2.0f}, {"compte", 2.0f}, {"virement", 2.0f}, {"montant", 2.0f},
                {"règlement", 2.0f}, {"transaction", 2.0f}, {"solde", 2.0f}, {"crédit", 2.0f},
                {"débit", 2.0f}, {"intérêt", 2.0f}, {"échéance", 2.0f}, {"devise", 2.0f},
                {"change", 2.0f}, {"taux", 2.0f}, {"frais", 2.0f}, {"commission", 2.0f},
                {"banque", 2.0f}, {"agence", 2.0f}, {"bénéficiaire", 2.0f}, {"remise", 2.0f},
                {"découvert", 2.0f}, {"dépôt", 2.0f}, {"versement", 2.0f},
                
                // Termes juridiques/contractuels
                {"accord", 1.5f}, {"contrat", 1.5f}, {"conditions", 1.5f}, {"clause", 1.5f},
                {"partie", 1.5f}, {"présent", 1.5f}, {"disposition", 1.5f}, {"conformément", 1.5f},
                {"stipulation", 1.5f}, {"convention", 1.5f},
                
                // Mots courants mais importants
                {"le", 0.5f}, {"la", 0.5f}, {"les", 0.5f}, {"et", 0.5f}, {"de", 0.5f},
                {"à", 0.5f}, {"dans", 0.5f}, {"pour", 0.5f}, {"par", 0.5f}, {"sur", 0.5f}
            }}
        };

        public int DetectLanguage(string text)
        {
            text = text.ToLower();
            var languageScores = new Dictionary<int, float>();

            // Initialiser les scores
            foreach (var lang in _languagePatterns.Keys)
            {
                languageScores[lang] = 0f;
            }

            // Calculer les scores pondérés pour chaque langue
            foreach (var langKvp in _languagePatterns)
            {
                int language = langKvp.Key;
                var patterns = langKvp.Value;

                foreach (var pattern in patterns)
                {
                    // Compter les occurrences du mot
                    int occurrences = CountOccurrences(text, pattern.Key);
                    if (occurrences > 0)
                    {
                        languageScores[language] += pattern.Value * occurrences;
                    }
                }
            }

            // Normaliser les scores
            float maxScore = languageScores.Values.Max();
            if (maxScore < 1.0f) // Seuil minimal pour la détection
            {
                return 0; // Langue inconnue
            }

            return languageScores.OrderByDescending(x => x.Value).First().Key;
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, System.StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }
    }
}
