using Graylog2Grafana.SimpleJsonGrafanaPlugin.Models;
using System.Text.Json.Serialization;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request
{
    public class AnnotationsResponse
    {
        /// <summary>
        /// The original annotation sent from Grafana.
        /// </summary>
        [JsonPropertyName("annotation")]
        public AnnotationItem Annotation { get; set; }

        /// <summary>
        /// Time since UNIX Epoch in milliseconds. (required)
        /// </summary>
        [JsonPropertyName("time")]
        public long Time { get; set; }

        /// <summary>
        /// The title for the annotation tooltip. (required)
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// // Tags for the annotation. (optional)
        /// </summary>
        [JsonPropertyName("tags")]
        public string Tags { get; set; }

        /// <summary>
        /// // Text for the annotation. (optional)
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}