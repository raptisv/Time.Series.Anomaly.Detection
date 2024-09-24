using System.Text.Json.Serialization;

namespace Graylog2Grafana.SimpleJsonGrafanaPlugin.Models
{
    public class AnnotationItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("iconColor")]
        public string IconColor { get; set; }

        [JsonPropertyName("enable")]
        public bool Enable { get; set; }

        [JsonPropertyName("query")]
        public string Query { get; set; }
    }
}
