using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class MonitorSources
    {
        public int ID { get; set; }

        [Required]
        [RegularExpression(@"^\w+$", ErrorMessage = "Allowed characters are a_z 0_9 and _")]
        public string Name { get; set; }

        [Required]
        public SourceType SourceType { get; set; }

        /// <summary>
        /// That is the base url if source type is Graylog
        /// </summary>
        [Required]
        public string Source { get; set; } = "http://localhost:9000";

        public string Username { get; set; } = "admin";

        public string Password { get; set; } = "admin";

        [Range(10, int.MaxValue)]
        public int LoadDataIntervalSeconds { get; set; } = 30; // 30 seconds

        [Range(0, int.MaxValue)]
        public int DetectionDelayInMinutes { get; set; } = 1; // 1 minute

        [Range(60, int.MaxValue)]
        public int DataRetentionInMinutes { get; set; } = 20160; // 14 days

        public DateTime LastTimestamp { get; set; }

        [JsonIgnore]
        public virtual List<MonitorSeries> MonitorSeriesData { get; set; }
    }
}