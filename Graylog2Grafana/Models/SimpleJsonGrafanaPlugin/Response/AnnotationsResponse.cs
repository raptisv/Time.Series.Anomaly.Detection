using Graylog.Grafana.SimpleJsonGrafanaPlugin.Models;
using Newtonsoft.Json;

namespace Graylog.Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class AnnotationsResponse
    {
        /// <summary>
        /// The original annotation sent from Grafana.
        /// </summary>
        [JsonProperty("annotation")]
        public AnnotationItem Annotation { get; set; }

        /// <summary>
        /// Time since UNIX Epoch in milliseconds. (required)
        /// </summary>
        [JsonProperty("time")]
        public long Time { get; set; }

        /// <summary>
        /// The title for the annotation tooltip. (required)
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// // Tags for the annotation. (optional)
        /// </summary>
        [JsonProperty("tags")]
        public string Tags { get; set; }

        /// <summary>
        /// // Text for the annotation. (optional)
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}