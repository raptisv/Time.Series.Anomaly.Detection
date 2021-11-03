using Graylog2Grafana.Abstractions;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Graylog2Grafana.Workers
{
    public class AnomalyDetectionWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IDataService> _dataServices;
        private readonly INotificationService _notificationService;

        public AnomalyDetectionWorker(
            ILogger logger,
            IEnumerable<IDataService> dataServices,
            INotificationService notificationService)
        {
            _logger = logger;
            _dataServices = dataServices;
            _notificationService = notificationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Delay 1 minute before starting anomaly detection
            await Task.Delay(60000).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Load data from all registered data services
                    foreach(var dataService in _dataServices)
                    {
                        // Detect & persist
                        var anomalyDetectionResult = await dataService.DetectAndPersistAnomaliesAsync();

                        // Alert if needed
                        foreach(var anomalyDetected in anomalyDetectionResult.Where(x => x.AnomalyDetectedAtLatestTimeStamp))
                        {
                            await _notificationService.NotifyAsync(
                                anomalyDetected.MonitorSeries, 
                                anomalyDetected.MonitorType,
                                anomalyDetected.LastDataInSeries,
                                anomalyDetected.PrelastDataInSeries);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error on {nameof(AnomalyDetectionWorker)}: {ex.Message}");
                }
                finally
                {
                    // Try detect and notify every 15 seconds
                    await Task.Delay(15000);
                }
            }
        }
    }
}
