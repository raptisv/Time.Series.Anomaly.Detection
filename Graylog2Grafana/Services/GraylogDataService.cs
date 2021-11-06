using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models;
using Graylog2Grafana.Models.Configuration;
using Graylog2Grafana.Models.Graylog;
using Graylog2Grafana.Models.Graylog.GraylogApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models.Enums;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog2Grafana.Services
{
    public class GraylogDataService : IDataService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMonitorSeriesDataAnomalyDetectionService _dataAnomalyDetectionService;
        private readonly IOptions<DatasetConfiguration> _detectionConfiguration;
        private readonly HttpClient _httpClient;
        private static bool? _graylogVersionSupportsViewSearchEndpoint = null;
        private readonly Stopwatch _sw;

        public GraylogDataService(
            ILogger logger,
            IServiceProvider serviceProvider,
            IHttpClientFactory clientFactory,
            IMonitorSeriesDataAnomalyDetectionService dataAnomalyDetectionService,
            IOptions<DatasetConfiguration> detectionConfiguration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dataAnomalyDetectionService = dataAnomalyDetectionService;
            _detectionConfiguration = detectionConfiguration;
            _httpClient = clientFactory.CreateClient("Graylog");
            _sw = new Stopwatch();
        }

        public async Task LoadDataAsync()
        {
            if (!_graylogVersionSupportsViewSearchEndpoint.HasValue)
            {
                _graylogVersionSupportsViewSearchEndpoint = await GraylogSupportsViewSearchEndpointAsync();
            }

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
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

                // 2. Execute
                var seriesData = _graylogVersionSupportsViewSearchEndpoint.Value
                    ? await GraylogQueryViewExecuteAsync(KnownIntervals.minute, queries)
                    : await GraylogQueryHistogramAsync(KnownIntervals.minute, queries);

                // 3. Persist
                foreach (var query in queries.Where(q => seriesData.ContainsKey(q.MonitorSeriesId.ToString())))
                {
                    var data = seriesData[query.MonitorSeriesId.ToString()];

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
                await monitorSeriesDataService.RemoveEntriesOlderThanMinutesAsync(_detectionConfiguration.Value.DataRetentionInMinutes);
            }
        }

        public async Task<List<DataAnomalyDetectionResult>> DetectAndPersistAnomaliesAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
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

                    var stepsBack = monitorSeries.MinuteDurationForAnomalyDetection + Math.Abs(_detectionConfiguration.Value.DetectionDelayInMinutes);

                    var monitorSeriesData = (await monitorSeriesDataService.GetLatestAsync(stepsBack, monitorSeries.ID))
                        .OrderBy(x => x.Timestamp)
                        .ToList();

                    _sw.Restart();

                    var anomalyDetected = _dataAnomalyDetectionService.DetectAnomaliesAsync(monitorSeries, monitorSeriesData);

                    _logger.Information($"Executed anomaly detection | {monitorSeries.Name} | Time {_sw.ElapsedMilliseconds} ms");

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

        private async Task<bool> GraylogSupportsViewSearchEndpointAsync()
        {
            try
            {
                List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)> dummyQueries =
                       new List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)>();

                // If that request succeeds, then it is supported
                await GraylogQueryViewExecuteAsync(KnownIntervals.minute, dummyQueries);

                _logger.Information($"--- The current Graylog version supports 'views/search' api endpoint ---");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, $"--- The current Graylog version does not support 'views/search' api endpoint ---");
                return false;
            }
        }

        private async Task<Dictionary<string, Dictionary<long, int>>> GraylogQueryHistogramAsync(
         KnownIntervals interval,
         List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)> queries)
        {
            Dictionary<string, Dictionary<long, int>> result = new Dictionary<string, Dictionary<long, int>>();

            foreach (var query in queries)
            {
                Dictionary<long, int> resultInner = await GraylogQueryHistogramAsync(query.Query, KnownIntervals.minute, query.DateFrom, query.DateTo);

                result.Add(query.MonitorSeriesId.ToString(), resultInner);
            }

            return result;
        }

        /// <summary>
        /// Supported from graylog versions older than 4
        /// </summary>
        /// 
        private async Task<Dictionary<long, int>> GraylogQueryHistogramAsync(
          string query,
          KnownIntervals interval,
          DateTime dtFrom,
          DateTime dtTo)
        {
            try
            {
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

                        return JsonConvert.DeserializeAnonymousType(strResult, new { results = new Dictionary<long, int>() }).results;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error loading Graylog query '{query}' | Error {ex.Message}");
                return new Dictionary<long, int>();
            }
        }

        /// <summary>
        /// Supported from graylog version 3.3 and above
        /// </summary>
        private async Task<Dictionary<string, Dictionary<long, int>>> GraylogQueryViewExecuteAsync(
          KnownIntervals interval,
          List<(long MonitorSeriesId, string Query, DateTime DateFrom, DateTime DateTo)> queries)
        {
            try
            {
                var searchId = "000000000000000000000AAA";

                var searchCreateRequest = new SearchCreateRequest(searchId, queries, interval);

                var searchCreateRequestPostBody = new StringContent(JsonConvert.SerializeObject(searchCreateRequest), Encoding.UTF8, "application/json");

                using (HttpResponseMessage searchCreateResponse = await _httpClient.PostAsync($"/api/views/search", searchCreateRequestPostBody))
                {
                    searchCreateResponse.EnsureSuccessStatusCode();

                    var searchExecuteRequestPostBody = new StringContent(JsonConvert.SerializeObject(new { }), Encoding.UTF8, "application/json");

                    using (HttpResponseMessage searchExecuteResponse = await _httpClient.PostAsync($"/api/views/search/{searchId}/execute", searchExecuteRequestPostBody))
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

                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error loading Graylog query | Error {ex.Message}");
                return new Dictionary<string, Dictionary<long, int>>();
            }
        }
    }
}
