using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Graylog2Grafana.Workers
{
    public class AnomalyDetectionWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IDataService> _dataServices;
        private readonly IOptions<DetectionConfiguration> _detectionConfiguration;

        public AnomalyDetectionWorker(
            ILogger logger,
            IEnumerable<IDataService> dataServices, 
            IOptions<DetectionConfiguration> detectionConfiguration)
        {
            _logger = logger;
            _dataServices = dataServices;
            _detectionConfiguration = detectionConfiguration;
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

                        // Detect, persist & alert
                        await dataService.DetectPersistAndAlertAsync();
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
