using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Graylog.Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class TimeSeriesRequest
    {
        [JsonProperty("panelId")]
        public string PanelId { get; set; }

        [JsonProperty("range")]
        public RangeItem Range { get; set; }

        [JsonProperty("rangeRaw")]
        public RangeRawItem RangeRaw { get; set; }

        [JsonProperty("interval")]
        public string Interval { get; set; }

        [JsonProperty("intervalMs")]
        public int IntervalMs { get; set; }

        [JsonProperty("targets")]
        public List<TargetItem> Targets { get; set; }

        [JsonProperty("adhocFilters")]
        public List<AdhocFilterItem> AdhocFilters { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("maxDataPoints")]
        public int MaxDataPoints { get; set; }

        public class RawItem
        {
            [JsonProperty("from")]
            public string From { get; set; }

            [JsonProperty("to")]
            public string To { get; set; }
        }

        public class RangeItem
        {
            [JsonProperty("from")]
            public DateTime From { get; set; }

            [JsonProperty("to")]
            public DateTime To { get; set; }

            [JsonProperty("raw")]
            public RawItem Raw { get; set; }
        }

        public class RangeRawItem
        {
            [JsonProperty("from")]
            public string From { get; set; }

            [JsonProperty("to")]
            public string To { get; set; }
        }

        public class TargetItem
        {
            [JsonProperty("target")]
            public string Target { get; set; }

            [JsonProperty("refId")]
            public string RefId { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }
        }

        public class AdhocFilterItem
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("operator")]
            public string Operator { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}
