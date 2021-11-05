using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Graylog2Grafana.Models.Graylog.GraylogApi
{
    public class SearchCreateRequest
    {
        public SearchCreateRequest(string id, List<(long queryId, string Query, DateTime DateFrom, DateTime DateTo)> queries, KnownIntervals interval)
        {
            Id = id;
            Queries = new List<QueryItem>()
            {
                new QueryItem(queries, interval)
            };
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("queries")]
        public List<QueryItem> Queries { get; set; }

        public class TimerangeRelative
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("from")]
            public int From { get; set; }
        }

        public class TimerangeAbsolute
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("from")]
            public string From { get; set; }

            [JsonProperty("to")]
            public string To { get; set; }
        }

        public class Filter
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("filters")]
            public List<object> Filters { get; set; }
        }

        public class Series
        {
            [JsonProperty("type")]
            public string Type { get; set; }
        }

        public class Interval
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("timeunit")]
            public string Timeunit { get; set; }
        }

        public class RowGroup
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("field")]
            public string Field { get; set; }

            [JsonProperty("interval")]
            public Interval Interval { get; set; }
        }

        public class SearchType
        {
            public SearchType(string queryId, string query, DateTime from, DateTime to, KnownIntervals interval)
            {
                Id = queryId;
                Timerange = new TimerangeAbsolute()
                {
                    Type = "absolute",
                    From = $"{from:yyyy-MM-ddTHH:mm:00.000}",
                    To = $"{to:yyyy-MM-ddTHH:mm:00.000}",
                };
                Query = new Query()
                {
                    Type = "elasticsearch",
                    QueryString = query
                };
                Series = new List<Series>()
                {
                    new Series()
                    {
                        Type = "count"
                    }
                };
                Rollup = true;
                Type = "pivot";

                string strInterval;
                switch (interval)
                {
                    case KnownIntervals.day:
                        strInterval = "1d";
                        break;
                    case KnownIntervals.hour:
                        strInterval = "1h";
                        break;
                    case KnownIntervals.minute:
                    default:
                        strInterval = "1m";
                        break;
                }

                RowGroups = new List<RowGroup>()
                {
                    new RowGroup()
                    {
                        Type = "time",
                        Field = "timestamp",
                        Interval = new Interval()
                        {
                            Type = "timeunit",
                            Timeunit = strInterval
                        }
                    }
                };
            }

            [JsonProperty("timerange")]
            public TimerangeAbsolute Timerange { get; }

            [JsonProperty("query")]
            public Query Query { get; }

            [JsonProperty("id")]
            public string Id { get; }

            [JsonProperty("series")]
            public List<Series> Series { get; }

            [JsonProperty("rollup")]
            public bool Rollup { get; }

            [JsonProperty("type")]
            public string Type { get; }

            [JsonProperty("row_groups")]
            public List<RowGroup> RowGroups { get; }
        }

        public class Query
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("query_string")]
            public string QueryString { get; set; }
        }

        public class QueryItem
        {
            public QueryItem(List<(long queryId, string Query, DateTime DateFrom, DateTime DateTo)> queries, KnownIntervals interval)
            {
                Id = "result_id";
                QueryData = new Query()
                {
                    Type = "elasticsearch",
                    QueryString = ""
                };
                Timerange = new TimerangeRelative()
                {
                    Type = "relative",
                    From = 1
                };
                Filter = new Filter()
                {
                    Type = "or",
                    Filters = new List<object>()
                };
                SearchTypes = queries.Select(x => new SearchType(x.queryId.ToString(), x.Query, x.DateFrom, x.DateTo, interval)).ToList();
            }

            [JsonProperty("id")]
            public string Id { get; }

            [JsonProperty("timerange")]
            public TimerangeRelative Timerange { get; }

            [JsonProperty("filter")]
            public Filter Filter { get; }

            [JsonProperty("query")]
            public Query QueryData { get; }

            [JsonProperty("search_types")]
            public List<SearchType> SearchTypes { get; }
        }
    }
}
