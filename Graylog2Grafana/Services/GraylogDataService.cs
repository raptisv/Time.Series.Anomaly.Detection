using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models;
using Graylog2Grafana.Models.Graylog;
using Graylog2Grafana.Models.Graylog.GraylogApi;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog2Grafana.Services
{
    public class GraylogDataService : IDataService
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMonitorSeriesDataAnomalyDetectionService _dataAnomalyDetectionService;

        private static string _graylogSearchId = Utils.GetRandomHexNumber(24);

        /// <summary>
        /// Helper property to check once, if the current graylog source supports View Search Endpoint or not.
        /// </summary>
        private ConcurrentDictionary<int, bool> _graylogVersionSupportsViewSearchEndpoint;

        public GraylogDataService(
            ILogger logger,
            IServiceProvider serviceProvider,
            IHttpClientFactory clientFactory,
            IMonitorSeriesDataAnomalyDetectionService dataAnomalyDetectionService)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _serviceProvider = serviceProvider;
            _dataAnomalyDetectionService = dataAnomalyDetectionService;

            _graylogVersionSupportsViewSearchEndpoint = new ConcurrentDictionary<int, bool>();
        }

        public async Task LoadDataAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IMonitorSourcesService monitorSourcesService = scope.ServiceProvider.GetRequiredService<IMonitorSourcesService>();

                var graylogSources = (await monitorSourcesService.GetAllAsync())
                .Where(x => x.SourceType == SourceType.Graylog);

                foreach (var graylogSource in graylogSources)
                {
                    var now = DateTime.UtcNow;

                    if (graylogSource.LastTimestamp.AddSeconds(graylogSource.LoadDataIntervalSeconds) <= now)
                    {
                        // It is time...
                        await LoadDataAsync(scope, graylogSource);

                        // Update last execution timestamp
                        await monitorSourcesService.UpdateLastTimestampAsync(graylogSource.ID, now);
                    }
                }
            }
        }

        public async Task<List<DataAnomalyDetectionResult>> DetectAndPersistAnomaliesAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IMonitorSourcesService monitorSourcesService = scope.ServiceProvider.GetRequiredService<IMonitorSourcesService>();

                var result = new List<DataAnomalyDetectionResult>();

                var graylogSources = (await monitorSourcesService.GetAllAsync())
                    .Where(x => x.SourceType == SourceType.Graylog);

                foreach (var graylogSource in graylogSources)
                {
                    var sourceResult = await DetectAndPersistAnomaliesAsync(scope, graylogSource);

                    result.AddRange(sourceResult);
                }

                return result;
            }
        }

        private async Task LoadDataAsync(
            IServiceScope scope,
            MonitorSources graylogSource)
        {
            // At first (and only once) define the source api (if it supports View Search Endpoint)
            var isSourceDefined = _graylogVersionSupportsViewSearchEndpoint.ContainsKey(graylogSource.ID);
            if (!isSourceDefined)
            {
                var graylogVersionSupportsViewSearchEndpoint = await GraylogSupportsViewSearchEndpointAsync(graylogSource);
                _graylogVersionSupportsViewSearchEndpoint[graylogSource.ID] = graylogVersionSupportsViewSearchEndpoint;
            }

            IMonitorSeriesService monitorSeriesService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesService>();
            IMonitorSeriesDataService monitorSeriesDataService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesDataService>();

            var monitorSeriesItems = await monitorSeriesService.GetAllAsync();

            // 1. Gather queries to be executed
            List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)> queries =
                new List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)>();

            foreach (var monitorSeries in monitorSeriesItems)
            {
                var monitorSeriesLatestRecord = (await monitorSeriesDataService.GetLatestAsync(1, monitorSeries.ID)).SingleOrDefault();

                var minutesBack = GetMinutesToLookBack(monitorSeriesLatestRecord?.Timestamp, monitorSeries.MinuteDurationForAnomalyDetection);

                DateTime dtFrom = Utils.TruncateToMinute(DateTime.UtcNow.AddMinutes(-minutesBack));
                DateTime dtTo = Utils.TruncateToMinute(DateTime.UtcNow.AddMinutes(1));

                queries.Add((monitorSeries.ID, monitorSeries.Query, dtFrom, dtTo));
            }

            if (!queries.Any())
            {
                _logger.Information($"No queries for source '{graylogSource.Name}'");
                return;
            }

            // 2. Execute
            var seriesResult = _graylogVersionSupportsViewSearchEndpoint[graylogSource.ID]
                ? await GraylogQueryViewExecuteAsync(graylogSource, KnownIntervals.minute, queries)
                : await GraylogQueryHistogramAsync(graylogSource, queries);

            // 3. Persist
            foreach (var query in queries.Where(q => seriesResult.Data.ContainsKey(q.MonitorSeriesId.ToString())))
            {
                var data = seriesResult.Data[query.MonitorSeriesId.ToString()];

                // Fill empty timeslots in range
                for (var timestamp = query.DateFrom; timestamp < query.DateTo; timestamp = timestamp.AddMinutes(1))
                {
                    var t = Utils.GetUnixTimestamp(timestamp);

                    if (!data.ContainsKey(t))
                    {
                        data[t] = 0;
                    }
                }

                foreach (var d in data)
                {
                    var date = Utils.GetDateFromUnixTimestamp(d.Key.ToString()).Value;
                    await monitorSeriesDataService.CreateOrUpdateByTimestampAsync(query.MonitorSeriesId, date, d.Value);
                }
            }

            // Retention policy
            await monitorSeriesDataService.RemoveEntriesOlderThanMinutesAsync(graylogSource.DataRetentionInMinutes);
        }

        private async Task<List<DataAnomalyDetectionResult>> DetectAndPersistAnomaliesAsync(
            IServiceScope scope,
            MonitorSources graylogSource)
        {
            IMonitorSeriesService monitorSeriesService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesService>();
            IMonitorSeriesDataService monitorSeriesDataService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesDataService>();
            IAnomalyDetectionDataService anomalyDetectionRecordService = scope.ServiceProvider.GetRequiredService<IAnomalyDetectionDataService>();

            List<DataAnomalyDetectionResult> result = new List<DataAnomalyDetectionResult>();

            var allMonitors = await monitorSeriesService.GetAllAsync();

            foreach (var monitorSeries in allMonitors)
            {
                // Check if another alert for series is very close
                if (monitorSeries.DoNotAlertAgainWithinMinutes.HasValue)
                {
                    var latestDetectionForSeries = await anomalyDetectionRecordService.GetLatestForSeriesAsync(monitorSeries.ID);
                    if (latestDetectionForSeries != null &&
                        (DateTime.UtcNow - latestDetectionForSeries.Timestamp).TotalMinutes < Math.Abs(monitorSeries.DoNotAlertAgainWithinMinutes.Value))
                    {
                        continue;
                    }
                }

                var stepsBack = monitorSeries.MinuteDurationForAnomalyDetection + Math.Abs(graylogSource.DetectionDelayInMinutes);

                var monitorSeriesData = (await monitorSeriesDataService.GetLatestAsync(stepsBack, monitorSeries.ID))
                    .OrderBy(x => x.Timestamp)
                    .ToList();

                var anomalyDetected = _dataAnomalyDetectionService.DetectAnomalies(monitorSeries, monitorSeriesData);

                if (anomalyDetected?.AnomalyDetectedAtLatestTimeStamp ?? false)
                {
                    var percentage = anomalyDetected.PrelastDataInSeries > 0
                            ? Math.Abs(anomalyDetected.LastDataInSeries - anomalyDetected.PrelastDataInSeries) / anomalyDetected.PrelastDataInSeries * 100.0
                            : 100;

                    var comment = anomalyDetected.MonitorType == MonitorType.Downwards
                        ? $"{(int)percentage}% downwards spike on {monitorSeries.Name} ({anomalyDetected.PrelastDataInSeries} => {anomalyDetected.LastDataInSeries})"
                        : $"{(int)percentage}% upwards spike on {monitorSeries.Name} ({anomalyDetected.PrelastDataInSeries} => {anomalyDetected.LastDataInSeries})";

                    // Persist

                    var alertCreated = await anomalyDetectionRecordService.CreateIfNotAlreadyExistsAsync(
                        monitorSeries.ID,
                        anomalyDetected.TimestampDetected,
                        anomalyDetected.MonitorType,
                        comment);

                    if (alertCreated)
                    {
                        result.Add(anomalyDetected);
                    }
                }
            }

            return result;
        }

        private int GetMinutesToLookBack(
            DateTime? monitorSeriesLatestRecordTimeStamp,
            int minuteDurationForAnomalyDetection,
            int extraCap = 5)
        {
            if (monitorSeriesLatestRecordTimeStamp.HasValue)
            {
                var minutesLastValueBefore = (int)(DateTime.UtcNow - monitorSeriesLatestRecordTimeStamp.Value).TotalMinutes;

                if (minutesLastValueBefore < 0)
                {
                    minutesLastValueBefore = 0;
                }

                minutesLastValueBefore += extraCap;

                // Make sure we do not exceed the minuteDurationForAnomalyDetection (plus any cap)
                if (minutesLastValueBefore > minuteDurationForAnomalyDetection + extraCap)
                {
                    minutesLastValueBefore = minuteDurationForAnomalyDetection + extraCap;
                }

                return minutesLastValueBefore;
            }
            else
            {
                // This will be executed only the first time that the series is empty
                return Math.Abs(minuteDurationForAnomalyDetection) + extraCap;
            }
        }

        private async Task<bool> GraylogSupportsViewSearchEndpointAsync(MonitorSources graylogSource)
        {
            List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)> dummyQueries =
                       new List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)>();

            // If that request succeeds, then it is supported
            var response = await GraylogQueryViewExecuteAsync(graylogSource, KnownIntervals.minute, dummyQueries);

            if (response.Success)
            {
                _logger.Information($"--- Source '{graylogSource.Name}' Graylog version supports 'views/search' api endpoint ---");
            }
            else
            {
                _logger.Warning($"--- Source '{graylogSource.Name}' Graylog version does not support 'views/search' api endpoint. Falling back to Histogram ---");
            }

            return response.Success;
        }

        private async Task<(bool Success, Dictionary<string, Dictionary<long, int>> Data)> GraylogQueryHistogramAsync(
            MonitorSources graylogSource,
            List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)> queries)
        {
            Dictionary<string, Dictionary<long, int>> result = new Dictionary<string, Dictionary<long, int>>();

            foreach (var query in queries)
            {
                var resultInner = await GraylogQueryHistogramAsync(graylogSource, query.Query, KnownIntervals.minute, query.DateFrom, query.DateTo);

                if (resultInner.Success)
                {
                    result.Add(query.MonitorSeriesId.ToString(), resultInner.Data);
                }
            }

            return (true, result);
        }

        /// <summary>
        /// Supported from graylog versions older than 4
        /// </summary>
        private async Task<(bool Success, Dictionary<long, int> Data)> GraylogQueryHistogramAsync(
          MonitorSources graylogSource,
          string query,
          KnownIntervals interval,
          DateTime dtFrom,
          DateTime dtTo)
        {
            try
            {
                var _httpClient = _clientFactory.CreateClient("Graylog");
                string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{graylogSource.Username}:{graylogSource.Password}"));
                _httpClient.BaseAddress = new Uri(graylogSource.Source.Trim('/'));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);

                string timestampFilter = $@" timestamp:[""{dtFrom:yyyy-MM-dd HH:mm:00.000}"" TO ""{dtTo:yyyy-MM-dd HH:mm:00.000}""] ";

                query += $" {(string.IsNullOrWhiteSpace(query) ? string.Empty : "AND")} {timestampFilter}";

                var parameters = new NameValueCollection
                {
                    {"query", query},
                    {"interval", interval.ToString()}
                };

                string AttachParameters(NameValueCollection parameters)
                {
                    var stringBuilder = new StringBuilder();
                    string str = "?";
                    for (int index = 0; index < parameters.Count; ++index)
                    {
                        stringBuilder.Append(str + parameters.AllKeys[index] + "=" + parameters[index]);
                        str = "&";
                    }
                    return stringBuilder.ToString();
                }

                using (HttpResponseMessage response = await _httpClient.GetAsync($"/api/search/universal/relative/histogram{AttachParameters(parameters)}"))
                {
                    response.EnsureSuccessStatusCode();

                    using (HttpContent content = response.Content)
                    {
                        string strResult = await content.ReadAsStringAsync();

                        return (true, JsonConvert.DeserializeAnonymousType(strResult, new { results = new Dictionary<long, int>() }).results);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error loading Graylog query '{query}' | Error {ex.Message}");
                return (false, new Dictionary<long, int>());
            }
        }

        /// <summary>
        /// Supported from graylog version 3.3 and above
        /// </summary>
        private async Task<(bool Success, Dictionary<string, Dictionary<long, int>> Data)> GraylogQueryViewExecuteAsync(
          MonitorSources graylogSource,
          KnownIntervals interval,
          List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)> queries)
        {
            try
            {
                var _httpClient = _clientFactory.CreateClient("Graylog");
                string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{graylogSource.Username}:{graylogSource.Password}"));
                _httpClient.BaseAddress = new Uri(graylogSource.Source.Trim('/'));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);

                var searchCreateRequest = new SearchCreateRequest(_graylogSearchId, queries, interval);

                var searchCreateRequestPostBody = new StringContent(JsonConvert.SerializeObject(searchCreateRequest), Encoding.UTF8, "application/json");

                using (HttpResponseMessage searchCreateResponse = await _httpClient.PostAsync($"/api/views/search", searchCreateRequestPostBody))
                {
                    searchCreateResponse.EnsureSuccessStatusCode();

                    var searchExecuteRequestPostBody = new StringContent(JsonConvert.SerializeObject(new { }), Encoding.UTF8, "application/json");

                    using (HttpResponseMessage searchExecuteResponse = await _httpClient.PostAsync($"/api/views/search/{_graylogSearchId}/execute", searchExecuteRequestPostBody))
                    {
                        searchExecuteResponse.EnsureSuccessStatusCode();

                        using (HttpContent searchExecuteResponseContent = searchExecuteResponse.Content)
                        {
                            string strResult = await searchExecuteResponseContent.ReadAsStringAsync();

                            var searchExecuteResult = JsonConvert.DeserializeObject<SearchExecuteResult>(strResult);

                            if (!searchExecuteResult.Execution.Done)
                            {
                                throw new Exception("Graylog execution not done");
                            }

                            Dictionary<string, Dictionary<long, int>> result = new Dictionary<string, Dictionary<long, int>>();

                            foreach (var searchType in searchExecuteResult.Results.ResultId.SearchTypes)
                            {
                                Dictionary<long, int> resultInner = new Dictionary<long, int>();

                                foreach (var search in searchType.Value.Rows.Where(x => x.Key.Any() && x.Values.Any()))
                                {
                                    resultInner.Add(Utils.GetUnixTimestamp(search.Key.First()), search.Values.First().Value);
                                }

                                result.Add(searchType.Key, resultInner);
                            }

                            return (true, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error loading Graylog query | Error {ex.Message}");
                return (false, new Dictionary<string, Dictionary<long, int>>());
            }
        }
    }
}
