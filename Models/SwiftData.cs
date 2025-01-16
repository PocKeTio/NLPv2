using Microsoft.ML.Data;

namespace NLPv2.Models
{
    /// <summary>
    /// Represents raw data from the database (SWIFT text, Classification, Language)
    /// </summary>
    public class SwiftData
    {
        public string SWIFT { get; set; }
        public int Category { get; set; }
        public int Language { get; set; }
    }

    /// <summary>
    /// Input class for ML.NET (multi-class)
    /// </summary>
    public class SwiftInput
    {
        [LoadColumn(0)] 
        public string SWIFT { get; set; }

        [LoadColumn(1)] 
        public float Category { get; set; }

        [LoadColumn(2)] 
        public float Language { get; set; }
    }

    /// <summary>
    /// Output (prediction) class for ML.NET (multi-class)
    /// </summary>
    public class SwiftPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedLabel { get; set; }

        [ColumnName("Score")]
        public float[] Score { get; set; }
    }

    /// <summary>
    /// Represents the final classification result
    /// </summary>
    public class ClassificationResult
    {
        public int Category { get; set; }
        public int Language { get; set; }
        public Dictionary<int, float> Probabilities { get; set; }

        public ClassificationResult()
        {
            Probabilities = new Dictionary<int, float>();
        }
    }
}
