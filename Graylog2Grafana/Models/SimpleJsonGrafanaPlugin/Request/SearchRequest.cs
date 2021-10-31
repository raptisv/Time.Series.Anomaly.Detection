using Newtonsoft.Json;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class SearchRequest
    {
        [JsonProperty("target")]
        public string Target { get; set; }
    }
}
