using Graylog.Grafana.Models.Graylog;
using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Time.Series.Anomaly.Detection.Abstractions;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models.Enums;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog.Grafana.Services
{
    public class GraylogDataService : IDataService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INotificationService _notificationService;
        private readonly IAnomalyDetectionService _anomalyDetectionService;
        private readonly IOptions<GraylogConfiguration> _graylogConfiguration;
        private readonly HttpClient _httpClient;

        public GraylogDataService(
            IServiceProvider serviceProvider,
            IHttpClientFactory clientFactory, 
            INotificationService notificationService, 
            IAnomalyDetectionService anomalyDetectionService, 
            IOptions<GraylogConfiguration> graylogConfiguration)
        {
            _serviceProvider = serviceProvider;
            _httpClient = clientFactory.CreateClient("Graylog");
            _notificationService = notificationService;
            _anomalyDetectionService = anomalyDetectionService;
            _graylogConfiguration = graylogConfiguration;
        }

        public async Task LoadDataAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IMonitorSeriesService monitorSeriesService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesService>();
                IMonitorSeriesDataService monitorSeriesDataService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesDataService>();

                var monitorSeriesItems = await monitorSeriesService.GetAllAsync();

                foreach (var monitorSeriesItem in monitorSeriesItems)
                {
                    var minutesBack = await GetMinutesToLookBack(monitorSeriesDataService, monitorSeriesItem.ID);

                    DateTime dtFrom = DateTime.UtcNow.AddMinutes(-minutesBack - 1); // Get 1 minute older just for safety
                    dtFrom = new DateTime(dtFrom.Year, dtFrom.Month, dtFrom.Day, dtFrom.Hour, dtFrom.Minute, 0, 0);
                    DateTime dtTo = DateTime.UtcNow.AddMinutes(1);
                    dtTo = new DateTime(dtTo.Year, dtTo.Month, dtTo.Day, dtTo.Hour, dtTo.Minute, 0, 0);

                    var data = await GraylogQueryHistogramAsync(monitorSeriesItem.Query, KnownIntervals.minute, dtFrom, dtTo);

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
                        await monitorSeriesDataService.CreateOrUpdateByTimestampAsync(monitorSeriesItem.ID, date, d.Value);
                    }
                }

                // Retention policy, keep data for 3 days
                await monitorSeriesDataService.RemoveEntriesOlderThanAsync(3);
            }
        }

        private async Task<int> GetMinutesToLookBack(IMonitorSeriesDataService monitorSeriesDataService, long monitorPerMinuteItemID)
        {
            var monitorPerMinuteDataLastResult = (await monitorSeriesDataService.GetLatestAsync(1, new List<long>() { monitorPerMinuteItemID })).SingleOrDefault();

            // Get diff in minutes
            var minutesLastValueBefore = (int)(DateTime.UtcNow - (monitorPerMinuteDataLastResult?.Timestamp ?? DateTime.UtcNow.AddMinutes(-60))).TotalMinutes;

            if (minutesLastValueBefore < 0)
            {
                minutesLastValueBefore = 0;
            }

            // Add some minutes just to be sure
            minutesLastValueBefore += 5;

            // Make sure we do not exceed 1 hour
            if (minutesLastValueBefore > 60)
            {
                minutesLastValueBefore = 60;
            }

            return minutesLastValueBefore;
        }

        public async Task<Dictionary<long, int>> GraylogQueryHistogramAsync(
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
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();

                    return JsonConvert.DeserializeAnonymousType(result, new { results = new Dictionary<long, int>() }).results;
                }
            }
        }

        public async Task DetectPersistAndAlertAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IMonitorSeriesService monitorSeriesService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesService>();
                IMonitorSeriesDataService monitorSeriesDataService = scope.ServiceProvider.GetRequiredService<IMonitorSeriesDataService>();
                IAnomalyDetectionDataService anomalyDetectionRecordService = scope.ServiceProvider.GetRequiredService<IAnomalyDetectionDataService>();

                var allMonitors = await monitorSeriesService.GetAllAsync();

                foreach (var currentMonitor in allMonitors)
                {
                    var stepsBack = currentMonitor.MinuteDurationForAnomalyDetection;

                    var monitorSeriesData = await monitorSeriesDataService.GetLatestAsync(stepsBack, currentMonitor.ID);

                    // Remove current minute from the calculations as it is probably still in progress of gathering data
                    monitorSeriesData.RemoveAll(x => Utils.TruncateToMinute(x.Timestamp) == Utils.TruncateToMinute(DateTime.UtcNow));

                    if (monitorSeriesData.Count < 12)
                    {
                        continue;
                    }

                    var rowData = monitorSeriesData
                        .Select(x => new RowDataItem() { Timestamp = Utils.GetUnixTimestamp(x.Timestamp), Count = x.Count })
                        .OrderBy(x => x.Timestamp);

                    var result = _anomalyDetectionService.Detect(rowData, currentMonitor.Sensitivity);

                    // If the last minute is an anomaly, persist & notify
                    result.Series.Reverse();
                    var last = result.Series.First();
                    var preLast = result.Series.Skip(1).Take(1).First();
                    var isUpwardsSpike = last.Data > preLast.Data;
                    var isDownwardsSpike = last.Data < preLast.Data;

                    var isInAcceptedLowerLimit = true;
                    if (currentMonitor.LowerLimitToDetect.HasValue)
                    {
                        isInAcceptedLowerLimit = preLast.Data > currentMonitor.LowerLimitToDetect.Value && last.Data > currentMonitor.LowerLimitToDetect.Value;
                    }

                    var isInAcceptedUpperLimit = true;
                    if (currentMonitor.UpperLimitToDetect.HasValue)
                    {
                        isInAcceptedUpperLimit = preLast.Data < currentMonitor.UpperLimitToDetect.Value && last.Data < currentMonitor.UpperLimitToDetect.Value;
                    }

                    var shouldAlert =
                        last.IsAnomaly &&
                        last.Data != preLast.Data &&
                        isInAcceptedLowerLimit && // Do not bother for very small values
                        isInAcceptedUpperLimit && // Do not bother for very large values
                        ((currentMonitor.MonitorType == MonitorType.DownwardsAndUpwards) ||
                        (currentMonitor.MonitorType == MonitorType.Upwards && isUpwardsSpike) ||
                        (currentMonitor.MonitorType == MonitorType.Downwards && isDownwardsSpike));

                    if (shouldAlert)
                    {
                        var currentName = string.IsNullOrWhiteSpace(currentMonitor.Name) ? currentMonitor.Name : currentMonitor.Name;

                        var timeStamp = Utils.GetDateFromUnixTimestamp(last.TimeStamp).Value;
                        var sameAlertExists = await anomalyDetectionRecordService.ExistsByMonitorSeriesAndTimestampAsync(currentMonitor.ID, timeStamp);
                        if (!sameAlertExists)
                        {
                            var percentage = preLast.Data > 0
                                ? Math.Abs(last.Data - preLast.Data) / preLast.Data * 100.0
                                : 100;

                            var comment = isDownwardsSpike
                                ? $"{(int)percentage}% downwards spike on {currentName} ({preLast.Data} => {last.Data})"
                                : $"{(int)percentage}% upwards spike on {currentName} ({preLast.Data} => {last.Data})";

                            var annotationType = isUpwardsSpike
                                ? MonitorType.Upwards
                                : MonitorType.Downwards;

                            // Persist

                            await anomalyDetectionRecordService.PostAsync(currentMonitor.ID, timeStamp, annotationType, comment);

                            // Notify

                            var graylogUrl = $"{_graylogConfiguration.Value.Url}/search?q={HttpUtility.UrlEncode(currentMonitor.Query)}&rangetype=absolute&from={DateTime.UtcNow.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}&to={DateTime.UtcNow.AddMinutes(10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}";

                            var message = isDownwardsSpike
                                ? $"Downwards spike on *<{graylogUrl}|{currentName}>* ({preLast.Data} => {last.Data})"
                                : $"Upwards spike on *<{graylogUrl}|{currentName}>* ({preLast.Data} => {last.Data})";

                            await _notificationService?.NotifyAsync(message);
                        }
                    }
                }
            }
        }
    }
}
