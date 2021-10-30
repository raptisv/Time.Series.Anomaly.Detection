using Graylog2Grafana.Abstractions;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Graylog2Grafana.Workers
{
    public class DataLoaderWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IDataService> _dataServices;

        public DataLoaderWorker(
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
                        await dataService.LoadDataAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error on {nameof(DataLoaderWorker)}: {ex.Message}");
                }
                finally
                {
                    await Task.Delay(30000);
                }
            }
        }
    }
}
