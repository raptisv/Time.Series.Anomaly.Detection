using System;

namespace Time.Series.Anomaly.Detection.Models
{
    public class RowDataItem
    {
        public DateTime Timestamp { get; set; }
        public double Count { get; set; }
    }
}
