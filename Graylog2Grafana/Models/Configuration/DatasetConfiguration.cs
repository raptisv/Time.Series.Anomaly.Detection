namespace Graylog2Grafana.Models.Configuration
{
    public class DatasetConfiguration
    {
        public int LoadDataIntervalMs { get; set; }
        public int DetectionDelayInMinutes { get; set; }
        public int DataRetentionInMinutes { get; set; }
    }
}
