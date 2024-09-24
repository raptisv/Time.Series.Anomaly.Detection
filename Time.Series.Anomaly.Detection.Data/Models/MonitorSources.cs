using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
        public bool Enabled { get; set; }

        [Required]
        public SourceType SourceType { get; set; }

        /// <summary>
        /// That is the base url if source type is Graylog
        /// </summary>
        [Required]
        public string Source { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        [Range(10, int.MaxValue)]
        public int LoadDataIntervalSeconds { get; set; }

        [Range(0, int.MaxValue)]
        public int DetectionDelayInMinutes { get; set; }

        [Range(60, int.MaxValue)]
        public int DataRetentionInMinutes { get; set; }

        public DateTime LastTimestamp { get; set; }

        [JsonIgnore]
        public virtual List<MonitorSeries> MonitorSeriesData { get; set; }
    }
}