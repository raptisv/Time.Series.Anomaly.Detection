using Graylog2Grafana.Abstractions;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Graylog2Grafana.Workers
{
    public class LoadDataWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IDataService> _dataServices;

        public LoadDataWorker(
            ILogger logger,
            IEnumerable<IDataService> dataServices)
        {
            _logger = logger;
            _dataServices = dataServices;
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
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error on {nameof(LoadDataWorker)}: {ex.Message}");
                }
                finally
                {
                    var intervalMs = 5000; // Execute every 5 seconds. Revisit in the future if needed.

                    await Task.Delay(intervalMs);
                }
            }
        }
    }
}
