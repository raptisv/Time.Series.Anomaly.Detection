using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Graylog2Grafana.Models.Graylog.GraylogApi
{
    public class SearchExecuteResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("search_id")]
        public string SearchId { get; set; }

        [JsonProperty("execution")]
        public ExecutionItem Execution { get; set; }

        [JsonProperty("results")]
        public ResultItem Results { get; set; }

        public class ExecutionItem
        {
            [JsonProperty("done")]
            public bool Done { get; set; }

            [JsonProperty("cancelled")]
            public bool Cancelled { get; set; }

            [JsonProperty("completed_exceptionally")]
            public bool CompletedExceptionally { get; set; }
        }

        public class ResultItem
        {
            [JsonProperty("result_id")]
            public ResultIdItem ResultId { get; set; }

            public class ResultIdItem
            {
                [JsonProperty("search_types")]
                public Dictionary<string, SearchTypeResult> SearchTypes { get; set; }

                [JsonProperty("errors")]
                public List<object> Errors { get; set; }

                [JsonProperty("state")]
                public string State { get; set; }

                public class SearchTypeResult
                {
                    [JsonProperty("id")]
                    public string Id { get; set; }

                    [JsonProperty("rows")]
                    public List<RowItem> Rows { get; set; }

                    public class RowItem
                    {
                        [JsonProperty("key")]
                        public List<DateTime> Key { get; set; }

                        [JsonProperty("values")]
                        public List<ValueItem> Values { get; set; }

                        public class ValueItem
                        {
                            [JsonProperty("value")]
                            public decimal? Value { get; set; }
                        }
                    }
                }
            }
        }
    }
}
