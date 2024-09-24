using Graylog2Grafana.SimpleJsonGrafanaPlugin.Models;
using System.Text.Json.Serialization;
using System;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class AnnotationsRequest
    {
        [JsonPropertyName("range")]
        public RangeItem Range { get; set; }

        [JsonPropertyName("rangeRaw")]
        public RangeRawItem RangeRaw { get; set; }

        [JsonPropertyName("annotation")]
        public AnnotationItem Annotation { get; set; }

        public class RangeItem
        {
            [JsonPropertyName("from")]
            public DateTime From { get; set; }

            [JsonPropertyName("to")]
            public DateTime To { get; set; }
        }

        public class RangeRawItem
        {
            [JsonPropertyName("from")]
            public string From { get; set; }

            [JsonPropertyName("to")]
            public string To { get; set; }
        }
    }
}
