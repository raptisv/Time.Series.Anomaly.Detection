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
using System.Globalization;
using System.Linq;
using System.Net;
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
        private static bool? _graylogVersionSupportsHistogramEndpoint = null;

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
        }

        public async Task LoadDataAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IMonitorSeriesService monitorSeriesService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesService>();
                IMonitorSeriesDataService monitorSeriesDataService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesDataService>();

                var monitorSeriesItems = await monitorSeriesService.GetAllAsync();

                foreach (var monitorSeries in monitorSeriesItems)
                {
                    var monitorSeriesLatestRecord = (await monitorSeriesDataService.GetLatestAsync(1, monitorSeries.ID)).SingleOrDefault();

                    var minutesBack = GetMinutesToLookBack(monitorSeriesLatestRecord?.Timestamp, monitorSeries.MinuteDurationForAnomalyDetection);

                    DateTime dtFrom = DateTime.UtcNow.AddMinutes(-minutesBack);
                    dtFrom = new DateTime(dtFrom.Year, dtFrom.Month, dtFrom.Day, dtFrom.Hour, dtFrom.Minute, 0, 0);
                    DateTime dtTo = DateTime.UtcNow.AddMinutes(1);
                    dtTo = new DateTime(dtTo.Year, dtTo.Month, dtTo.Day, dtTo.Hour, dtTo.Minute, 0, 0);

                    try
                    {
                        if (!_graylogVersionSupportsHistogramEndpoint.HasValue)
                        {
                            _graylogVersionSupportsHistogramEndpoint = await GraylogSupportsHistogramEndpointAsync();
                        }

                        var data = _graylogVersionSupportsHistogramEndpoint.Value
                            ? await GraylogQueryHistogramAsync(monitorSeries.Query, KnownIntervals.minute, dtFrom, dtTo)
                            : await GraylogQueryViewExecuteAsync(monitorSeries.Query, KnownIntervals.minute, dtFrom, dtTo);

                        // Fill empty timeslots in range
                        for (var timestamp = dtFrom; timestamp < dtTo; timestamp = timestamp.AddMinutes(1))
                        {
                            var t = Utils.GetUnixTimestamp(timestamp);

                            if (!data.Any(x => x.Key == t))
                            {
                                data.Add(t, 0);
                            }
                        }

                        foreach (var d in data)
                        {
                            var date = Utils.GetDateFromUnixTimestamp(d.Key.ToString()).Value;
                            await monitorSeriesDataService.CreateOrUpdateByTimestampAsync(monitorSeries.ID, date, d.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error loading Graylog data for series '{monitorSeries.Name}' | Error {ex.Message}");
                    }
                }

                // Retention policy, keep data for 3 days
                await monitorSeriesDataService.RemoveEntriesOlderThanAsync(3);
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

                    var anomalyDetected = _dataAnomalyDetectionService.DetectAnomaliesAsync(monitorSeries, monitorSeriesData);

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
        private async Task<bool> GraylogSupportsHistogramEndpointAsync()
        {
            using (HttpResponseMessage response = await _httpClient.GetAsync($"/api/search/universal/relative/histogram?query=*&interval=day"))
            {
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                throw new Exception("Could not define if Graylog version supports query histogram");
            }
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

        /// <summary>
        /// Supported from graylog version 3.3 and above
        /// </summary>
        private async Task<Dictionary<long, int>> GraylogQueryViewExecuteAsync(
          string query,
          KnownIntervals interval,
          DateTime dtFrom,
          DateTime dtTo)
        {
            var searchId = "00000000000000000000000A";

            var searchCreateRequest = new SearchCreateRequest(searchId, query, $"{dtFrom:yyyy-MM-ddTHH:mm:00.000}", $"{dtTo:yyyy-MM-ddTHH:mm:00.000}", interval);

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

                        var obj = JsonConvert.DeserializeAnonymousType(strResult, new { results = new { result_id = new { search_types = new { result_id = new { rows = new[] { new { key = new string[1], values = new[] { new { value = 0 } } } } } } } } });

                        Dictionary<long, int> result = new Dictionary<long, int>();

                        foreach (var item in obj.results.result_id.search_types.result_id.rows.Where(x => x.key.Any() && x.values.Any()))
                        {
                            var dt = DateTime.ParseExact(item.key.First(), "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                            result.Add(Utils.GetUnixTimestamp(dt), item.values.First().value);
                        }

                        return result;
                    }
                }
            }
        }
    }
}
