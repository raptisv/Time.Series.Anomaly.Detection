using Newtonsoft.Json;
using System.Collections.Generic;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Response
{
    public class TimeSiriesReponseTargetItem
    {
        /// <summary>
        /// The field being queried for
        /// </summary>
        [JsonProperty("target")]
        public string Target { get; set; }

        /// <summary>
        /// Metric value as a float , unixtimestamp in milliseconds
        /// </summary>
        [JsonProperty("datapoints")]
        public List<List<object>> Datapoints { get; set; }
    }
}
