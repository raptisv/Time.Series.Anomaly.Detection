using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Graylog2Grafana.Models.Graylog.GraylogApi
{
    public class SearchExecuteResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("search_id")]
        public string SearchId { get; set; }

        [JsonPropertyName("execution")]
        public ExecutionItem Execution { get; set; }

        [JsonPropertyName("results")]
        public ResultItem Results { get; set; }

        public class ExecutionItem
        {
            [JsonPropertyName("done")]
            public bool Done { get; set; }

            [JsonPropertyName("cancelled")]
            public bool Cancelled { get; set; }

            [JsonPropertyName("completed_exceptionally")]
            public bool CompletedExceptionally { get; set; }
        }

        public class ResultItem
        {
            [JsonPropertyName("result_id")]
            public ResultIdItem ResultId { get; set; }

            public class ResultIdItem
            {
                [JsonPropertyName("search_types")]
                public Dictionary<string, SearchTypeResult> SearchTypes { get; set; }

                [JsonPropertyName("errors")]
                public List<object> Errors { get; set; }

                [JsonPropertyName("state")]
                public string State { get; set; }

                public class SearchTypeResult
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("rows")]
                    public List<RowItem> Rows { get; set; }

                    public class RowItem
                    {
                        [JsonPropertyName("key")]
                        public List<DateTime> Key { get; set; }

                        [JsonPropertyName("values")]
                        public List<ValueItem> Values { get; set; }

                        public class ValueItem
                        {
                            [JsonPropertyName("value")]
                            public decimal? Value { get; set; }
                        }
                    }
                }
            }
        }
    }
}
