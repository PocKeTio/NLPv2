namespace NLPv2.Common
{
    public class MLSettings
    {
        public string SaveModelPath { get; set; } = string.Empty;
        public int MaxIterations { get; set; } = 100;
        public float ConvergenceTolerance { get; set; } = 1e-3f;
        public int NgramLength { get; set; } = 3;
        public int HashBits { get; set; } = 16;
    }
}
