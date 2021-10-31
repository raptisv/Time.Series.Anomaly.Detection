using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
        private readonly IOptions<DetectionConfiguration> _detectionConfiguration;

        public AnomalyDetectionWorker(
            ILogger logger,
            IEnumerable<IDataService> dataServices,
            IOptions<DetectionConfiguration> detectionConfiguration, 
            INotificationService notificationService)
        {
            _logger = logger;
            _dataServices = dataServices;
            _detectionConfiguration = detectionConfiguration;
            _notificationService = notificationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Load data from all registered data services
                    foreach(var dataService in _dataServices)
                    {
                        // Fetch data
                        await dataService.LoadDataAsync();

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
                    // Sanity check, no point in setting this less that 10 seconds
                    var intervalMs = Math.Max(_detectionConfiguration.Value.IntervalMs, 10000); 

                    await Task.Delay(intervalMs);
                }
            }
        }
    }
}
