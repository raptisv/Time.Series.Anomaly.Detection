using Newtonsoft.Json;

namespace Graylog2Grafana.SimpleJsonGrafanaPlugin.Models
{
    public class AnnotationItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("datasource")]
        public string Datasource { get; set; }

        [JsonProperty("iconColor")]
        public string IconColor { get; set; }

        [JsonProperty("enable")]
        public bool Enable { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }
    }
}
