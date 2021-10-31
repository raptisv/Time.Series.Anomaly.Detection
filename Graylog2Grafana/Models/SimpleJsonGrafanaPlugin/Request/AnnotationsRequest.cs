using Graylog2Grafana.SimpleJsonGrafanaPlugin.Models;
using Newtonsoft.Json;
using System;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class AnnotationsRequest
    {
        [JsonProperty("range")]
        public RangeItem Range { get; set; }

        [JsonProperty("rangeRaw")]
        public RangeRawItem RangeRaw { get; set; }

        [JsonProperty("annotation")]
        public AnnotationItem Annotation { get; set; }

        public class RangeItem
        {
            [JsonProperty("from")]
            public DateTime From { get; set; }

            [JsonProperty("to")]
            public DateTime To { get; set; }
        }

        public class RangeRawItem
        {
            [JsonProperty("from")]
            public string From { get; set; }

            [JsonProperty("to")]
            public string To { get; set; }
        }
    }
}
