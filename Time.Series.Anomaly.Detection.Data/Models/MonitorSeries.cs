using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class MonitorSeries
    {
        public long ID { get; set; }

        [Required]
        [RegularExpression(@"^\w+$", ErrorMessage = "Allowed characters are a_z 0_9 and _")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string Query { get; set; }

        [Required]
        [Range(1, 100)]
        public double Sensitivity { get; set; }

        [Required]
        public MonitorType MonitorType { get; set; }

        [Required]
        [Range(20, 1000)]
        public int MinuteDurationForAnomalyDetection { get; set; }

        public int? LowerLimitToDetect { get; set; }

        public int? UpperLimitToDetect { get; set; }

        public virtual List<MonitorSeriesData> MonitorSeriesData { get; set; }
        public virtual List<AnomalyDetectionData> AnomalyDetectionData { get; set; }
    }
}
