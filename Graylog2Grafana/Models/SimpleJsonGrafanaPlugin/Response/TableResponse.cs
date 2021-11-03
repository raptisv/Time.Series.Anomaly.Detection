using Newtonsoft.Json;
using System.Collections.Generic;

namespace Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Response
{
    public class TableResponseTargetItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("columns")]
        public List<Column> Columns { get; set; }

        [JsonProperty("rows")]
        public List<List<object>> Rows { get; set; }

        public class Column
        {
            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }
        }
    }
}
