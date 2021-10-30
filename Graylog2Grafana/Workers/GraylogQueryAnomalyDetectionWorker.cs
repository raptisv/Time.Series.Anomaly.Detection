using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Time.Series.Anomaly.Detection.Abstractions;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models.Enums;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog2Grafana.Workers
{
    public class GraylogQueryAnomalyDetectionWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IDataService _dataService;
        private readonly INotificationService _notificationService;
        private readonly IAnomalyDetectionService _anomalyDetectionService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<GraylogConfiguration> _graylogConfiguration;

        public GraylogQueryAnomalyDetectionWorker(
            ILogger logger,
            IDataService dataService,
            INotificationService notificationService,
            IAnomalyDetectionService anomalyDetectionService,
            IServiceProvider serviceProvider, 
            IOptions<GraylogConfiguration> graylogConfiguration)
        {
            _logger = logger;
            _dataService = dataService;
            _notificationService = notificationService;
            _anomalyDetectionService = anomalyDetectionService;
            _serviceProvider = serviceProvider;
            _graylogConfiguration = graylogConfiguration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
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
                            var isUpwardsSpike = last.Data > last.Lower;
                            var isDownwardsSpike = last.Data < last.Lower;

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
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error on {nameof(GraylogQueryAnomalyDetectionWorker)}: {ex.Message}");
                }
                finally
                {
                    // Every 10 seconds
                    await Task.Delay(10000);
                }
            }
        }
    }
}
