using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Graylog2Grafana.Models.Graylog.GraylogApi
{
    public class SearchCreateRequest
    {
        public SearchCreateRequest(string id, List<(string queryId, string Query, DateTime DateFrom, DateTime DateTo, string Aggregation, string Field)> queries,
            KnownIntervals interval)
        {
            Id = id;
            Queries = new List<QueryItem>()
            {
                new QueryItem(queries, interval)
            };
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("queries")]
        public List<QueryItem> Queries { get; set; }

        public class TimerangeRelative
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("from")]
            public int From { get; set; }
        }

        public class TimerangeAbsolute
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("from")]
            public string From { get; set; }

            [JsonPropertyName("to")]
            public string To { get; set; }
        }

        public class Filter
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("filters")]
            public List<object> Filters { get; set; }
        }

        public class Series
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("field")]
            public string Field { get; set; }
        }

        public class Interval
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("timeunit")]
            public string Timeunit { get; set; }
        }

        public class RowGroup
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("field")]
            public string Field { get; set; }

            [JsonPropertyName("interval")]
            public Interval Interval { get; set; }
        }

        public class SearchType
        {
            public SearchType(
                string queryId,
                string query,
                DateTime from,
                DateTime to,
                KnownIntervals interval,
                string aggregationType,
                string field)
            {
                var series = new Series()
                {
                    Type = "count"
                };

                if (!string.IsNullOrWhiteSpace(field))
                {
                    series = new Series()
                    {
                        Type = aggregationType.ToString(),
                        Id = $"{aggregationType}({field.Trim()})",
                        Field = field.Trim()
                    };
                }

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
                    series
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

            [JsonPropertyName("timerange")]
            public TimerangeAbsolute Timerange { get; }

            [JsonPropertyName("query")]
            public Query Query { get; }

            [JsonPropertyName("id")]
            public string Id { get; }

            [JsonPropertyName("series")]
            public List<Series> Series { get; }

            [JsonPropertyName("rollup")]
            public bool Rollup { get; }

            [JsonPropertyName("type")]
            public string Type { get; }

            [JsonPropertyName("row_groups")]
            public List<RowGroup> RowGroups { get; }
        }

        public class Query
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("query_string")]
            public string QueryString { get; set; }
        }

        public class QueryItem
        {
            public QueryItem(List<(string queryId, string Query, DateTime DateFrom, DateTime DateTo, string Aggregation, string Field)> queries, KnownIntervals interval)
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
                SearchTypes = queries.Select(x =>
                {
                    return new SearchType(x.queryId, x.Query, x.DateFrom, x.DateTo, interval, x.Aggregation, x.Field);
                }).ToList();
            }

            [JsonPropertyName("id")]
            public string Id { get; }

            [JsonPropertyName("timerange")]
            public TimerangeRelative Timerange { get; }

            [JsonPropertyName("filter")]
            public Filter Filter { get; }

            [JsonPropertyName("query")]
            public Query QueryData { get; }

            [JsonPropertyName("search_types")]
            public List<SearchType> SearchTypes { get; }
        }
    }
}
