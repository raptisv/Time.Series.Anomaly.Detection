using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class MonitorSeriesForGroupValue : MonitorSeries
    {
        public string GroupValue { get; set; }
    }

    public class MonitorSeries
    {
        [JsonIgnore]
        public long ID { get; set; }

        [Required]
        //[RegularExpression(@"^\w+$", ErrorMessage = "Allowed characters are a_z 0_9 and _")]
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

        [Range(0, 10000)]
        public int? DoNotAlertAgainWithinMinutes { get; set; }

        [Required]
        public int MonitorSourceID { get; set; }

        [JsonIgnore]
        public MonitorSources MonitorSource { get; set; }

        [Required]
        public int MonitorGroupID { get; set; }

        [JsonIgnore]
        public MonitorGroups MonitorGroup { get; set; }

        [JsonIgnore]
        public virtual List<MonitorSeriesData> MonitorSeriesData { get; set; }

        [JsonIgnore]
        public virtual List<AnomalyDetectionData> AnomalyDetectionData { get; set; }

        // Aggregation

        [Required]
        public string Aggregation { get; set; }

        [RegularExpression(@"^\w+$", ErrorMessage = "Allowed characters are a_z 0_9 and _")]
        public string Field { get; set; }

        // Group

        [RegularExpression(@"^\w+$", ErrorMessage = "Allowed characters are a_z 0_9 and _")]
        public string GroupBy { get; set; }

        /// <summary>
        /// Comma separated
        /// </summary>
        public string GroupByValues { get; set; }
    }
}
