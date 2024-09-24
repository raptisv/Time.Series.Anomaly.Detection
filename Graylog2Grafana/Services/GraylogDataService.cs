using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models;
using Graylog2Grafana.Models.Graylog;
using Graylog2Grafana.Models.Graylog.GraylogApi;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Extensions;
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
        }

        public async Task LoadDataAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var monitorSourcesService = scope.ServiceProvider.GetRequiredService<IMonitorSourcesService>();

                var graylogSources = (await monitorSourcesService.GetAllAsync())
                .Where(x => x.SourceType == SourceType.Graylog);

                foreach (var graylogSource in graylogSources)
                {
                    if (!graylogSource.Enabled)
                    {
                        _logger.Warning($"Caution | Source '{graylogSource.Name}' is not enabled");
                        continue;
                    }

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
                    if (!graylogSource.Enabled)
                    {
                        _logger.Warning($"Caution | Source '{graylogSource.Name}' is not enabled");
                        continue;
                    }

                    var sourceResult = await DetectAndPersistAnomaliesAsync(scope, graylogSource);

                    result.AddRange(sourceResult);
                }

                return result;
            }
        }

        private async Task LoadDataAsync(IServiceScope scope, MonitorSources graylogSource)
        {
            IMonitorSeriesService monitorSeriesService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesService>();
            IMonitorSeriesDataService monitorSeriesDataService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesDataService>();

            var monitorSeriesItems = await monitorSeriesService.GetAllAsync();

            // 1. Gather queries to be executed
            List<(string QueryId, string Query, DateTime DateFrom, DateTime DateTo, string Aggregation, string Property)> queries =
                new List<(string QueryId, string Query, DateTime DateFrom, DateTime DateTo, string Aggregation, string Property)>();

            foreach (var monitorSeries in monitorSeriesItems.SelectMany(x => x.AllWithGrouping()))
            {
                var monitorSeriesLatestRecord = (await monitorSeriesDataService.GetLatestAsync(1, monitorSeries.ID, monitorSeries.GroupValue)).SingleOrDefault();

                var minutesBack = GetMinutesToLookBack(monitorSeriesLatestRecord?.Timestamp, monitorSeries.MinuteDurationForAnomalyDetection);

                DateTime dtFrom = Utils.TruncateToMinute(DateTime.UtcNow.AddMinutes(-minutesBack));
                DateTime dtTo = Utils.TruncateToMinute(DateTime.UtcNow.AddMinutes(1));

                // IMPORTANT! Do not change this. It will be used to identify the response values later.
                var queryID = monitorSeries.ID + "_" + monitorSeries.GroupValue;

                queries.Add((queryID, monitorSeries.Query, dtFrom, dtTo, monitorSeries.Aggregation, monitorSeries.Field));
            }

            if (!queries.Any())
            {
                _logger.Information($"No queries for source '{graylogSource.Name}'");
                return;
            }

            // 2. Execute
            var seriesResult = await GraylogQueryViewExecuteAsync(graylogSource, KnownIntervals.minute, queries);

            // 3. Persist
            foreach (var query in queries.Where(q => seriesResult.Data.ContainsKey(q.QueryId)))
            {
                var data = seriesResult.Data[query.QueryId];

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
                    var queryIdParts = query.QueryId.Split('_');
                    var monitorSeriesId = long.Parse(queryIdParts[0]);
                    var monitorSeriesGroupValue = queryIdParts.Length > 1 && !string.IsNullOrWhiteSpace(queryIdParts[1]) ? queryIdParts[1].Trim() : null;

                    var date = Utils.GetDateFromUnixTimestamp(d.Key.ToString()).Value;
                    await monitorSeriesDataService.CreateOrUpdateByTimestampAsync(monitorSeriesId, monitorSeriesGroupValue, date, d.Value);
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

            foreach (var monitorSeries in allMonitors.SelectMany(x => x.AllWithGrouping()))
            {
                // Check if another alert for series is very close
                if (monitorSeries.DoNotAlertAgainWithinMinutes.HasValue)
                {
                    var latestDetectionForSeries = await anomalyDetectionRecordService.GetLatestForSeriesAsync(monitorSeries.ID, monitorSeries.GroupValue);
                    if (latestDetectionForSeries != null &&
                        (DateTime.UtcNow - latestDetectionForSeries.Timestamp).TotalMinutes < Math.Abs(monitorSeries.DoNotAlertAgainWithinMinutes.Value))
                    {
                        continue;
                    }
                }

                var stepsBack = monitorSeries.MinuteDurationForAnomalyDetection + Math.Abs(graylogSource.DetectionDelayInMinutes);

                var monitorSeriesData = (await monitorSeriesDataService.GetLatestAsync(stepsBack, monitorSeries.ID, monitorSeries.GroupValue))
                    .OrderBy(x => x.Timestamp)
                    .ToList();

                var anomalyDetected = _dataAnomalyDetectionService.DetectAnomalies(monitorSeries, monitorSeriesData);

                if (anomalyDetected?.AnomalyDetectedAtLatestTimeStamp ?? false)
                {
                    var comment = anomalyDetected.MonitorType == MonitorType.Downwards
                        ? $"Downwards spike on {monitorSeries.Name} ({anomalyDetected.PrelastDataInSeries} => {anomalyDetected.LastDataInSeries})"
                        : $"Upwards spike on {monitorSeries.Name} ({anomalyDetected.PrelastDataInSeries} => {anomalyDetected.LastDataInSeries})";

                    // Persist

                    var alertCreated = await anomalyDetectionRecordService.CreateIfNotAlreadyExistsAsync(
                        monitorSeries.ID,
                        monitorSeries.GroupValue,
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
            List<(string QueryId, string Query, DateTime DateFrom, DateTime DateTo, string Aggregation, string Property)> dummyQueries =
                       new List<(string QueryId, string Query, DateTime DateFrom, DateTime DateTo, string Aggregation, string Property)>();

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

        /// <summary>
        /// Supported from graylog version 3.3 and above
        /// </summary>
        private async Task<(bool Success, Dictionary<string, Dictionary<long, decimal>> Data)> GraylogQueryViewExecuteAsync(
          MonitorSources graylogSource,
          KnownIntervals interval,
          List<(string QueryId, string Query, DateTime DateFrom, DateTime DateTo, string Aggregation, string Property)> queries)
        {
            try
            {
                var _httpClient = _clientFactory.CreateClient("Graylog");
                string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{graylogSource.Username}:{graylogSource.Password}"));
                _httpClient.BaseAddress = new Uri(graylogSource.Source.Trim('/'));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);

                var searchCreateRequest = new SearchCreateRequest(_graylogSearchId, queries, interval);

                var strRequest = JsonSerializer.Serialize(searchCreateRequest);

                var searchCreateRequestPostBody = new StringContent(strRequest, Encoding.UTF8, "application/json");

                using (HttpResponseMessage searchCreateResponse = await _httpClient.PostAsync($"/api/views/search", searchCreateRequestPostBody))
                {
                    var strResponse = await searchCreateResponse.Content.ReadAsStringAsync();

                    searchCreateResponse.EnsureSuccessStatusCode();

                    var searchExecuteRequestPostBody = new StringContent(JsonSerializer.Serialize(new { }), Encoding.UTF8, "application/json");

                    using (HttpResponseMessage searchExecuteResponse = await _httpClient.PostAsync($"/api/views/search/{_graylogSearchId}/execute", searchExecuteRequestPostBody))
                    {
                        searchExecuteResponse.EnsureSuccessStatusCode();

                        using (HttpContent searchExecuteResponseContent = searchExecuteResponse.Content)
                        {
                            string strResult = await searchExecuteResponseContent.ReadAsStringAsync();

                            var searchExecuteResult = JsonSerializer.Deserialize<SearchExecuteResult>(strResult);

                            if (!searchExecuteResult.Execution.Done)
                            {
                                throw new Exception("Graylog execution not done");
                            }

                            Dictionary<string, Dictionary<long, decimal>> result = new Dictionary<string, Dictionary<long, decimal>>();

                            foreach (var searchType in searchExecuteResult.Results.ResultId.SearchTypes)
                            {
                                Dictionary<long, decimal> resultInner = new Dictionary<long, decimal>();

                                foreach (var search in searchType.Value.Rows.Where(x => x.Key.Any() && x.Values.Any()))
                                {
                                    resultInner.Add(Utils.GetUnixTimestamp(search.Key.First()), search.Values.First().Value ?? 0);
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
                return (false, new Dictionary<string, Dictionary<long, decimal>>());
            }
        }
    }
}
