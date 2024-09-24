using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Response
{
    public class TableResponseTargetItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("columns")]
        public List<Column> Columns { get; set; }

        [JsonPropertyName("rows")]
        public List<List<object>> Rows { get; set; }

        public class Column
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
        }
    }
}
