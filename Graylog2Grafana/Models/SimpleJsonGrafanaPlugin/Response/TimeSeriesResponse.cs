using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Response
{
    public class TimeSeriesReponseTargetItem
    {
        /// <summary>
        /// The field being queried for
        /// </summary>
        [JsonPropertyName("target")]
        public string Target { get; set; }

        /// <summary>
        /// Metric value as a float , unixtimestamp in milliseconds
        /// </summary>
        [JsonPropertyName("datapoints")]
        public List<List<object>> Datapoints { get; set; }
    }
}
