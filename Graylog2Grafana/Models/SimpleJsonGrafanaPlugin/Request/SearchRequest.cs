using System.Text.Json.Serialization;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class SearchRequest
    {
        [JsonPropertyName("target")]
        public string Target { get; set; }
    }
}
