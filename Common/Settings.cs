namespace NLPv2.Common
{
    public class DatabaseSettings
    {
        public string Path { get; set; }
        public string TableName { get; set; }
    }

    public class ModelSettings
    {
        public float StatisticalWeight { get; set; }
        public string SaveModelPath { get; set; }
    }
}
