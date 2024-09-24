using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class TimeSeriesRequest
    {
        [JsonPropertyName("panelId")]
        public string PanelId { get; set; }

        [JsonPropertyName("range")]
        public RangeItem Range { get; set; }

        [JsonPropertyName("rangeRaw")]
        public RangeRawItem RangeRaw { get; set; }

        [JsonPropertyName("interval")]
        public string Interval { get; set; }

        [JsonPropertyName("intervalMs")]
        public int IntervalMs { get; set; }

        [JsonPropertyName("targets")]
        public List<TargetItem> Targets { get; set; }

        [JsonPropertyName("adhocFilters")]
        public List<AdhocFilterItem> AdhocFilters { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("maxDataPoints")]
        public int MaxDataPoints { get; set; }

        public class RawItem
        {
            [JsonPropertyName("from")]
            public string From { get; set; }

            [JsonPropertyName("to")]
            public string To { get; set; }
        }

        public class RangeItem
        {
            [JsonPropertyName("from")]
            public DateTime From { get; set; }

            [JsonPropertyName("to")]
            public DateTime To { get; set; }

            [JsonPropertyName("raw")]
            public RawItem Raw { get; set; }
        }

        public class RangeRawItem
        {
            [JsonPropertyName("from")]
            public string From { get; set; }

            [JsonPropertyName("to")]
            public string To { get; set; }
        }

        public class TargetItem
        {
            [JsonPropertyName("target")]
            public string Target { get; set; }

            [JsonPropertyName("refId")]
            public string RefId { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
        }

        public class AdhocFilterItem
        {
            [JsonPropertyName("key")]
            public string Key { get; set; }

            [JsonPropertyName("operator")]
            public string Operator { get; set; }

            [JsonPropertyName("value")]
            public string Value { get; set; }
        }
    }
}
