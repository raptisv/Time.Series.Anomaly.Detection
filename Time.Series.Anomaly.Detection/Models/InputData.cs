using Microsoft.ML.Data;
using System;

namespace Time.Series.Anomaly.Detection.Models
{
    public class InputData
    {
        [LoadColumn(0)]
        public string t { get; set; }

        [LoadColumn(1)]
        public double v { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
