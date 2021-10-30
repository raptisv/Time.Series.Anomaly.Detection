using System;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class MonitorSeriesData 
    {
        public long ID { get; set; }
        public long MonitorSeriesID { get; set; }
        public MonitorSeries MonitorSeries { get; set; }
        public DateTime Timestamp { get; set; }
        public double Count { get; set; }
    }
}
