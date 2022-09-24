using System;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class AnomalyDetectionData
    {
        public long ID { get; set; }
        public long MonitorSeriesID { get; set; }
        public string MonitorSeriesGroupValue { get; set; }
        public MonitorSeries MonitorSeries { get; set; }
        public DateTime Timestamp { get; set; }
        public MonitorType MonitorType { get; set; }        
        public string Comments{ get; set; }
    }
}
