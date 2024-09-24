using Graylog2Grafana.Abstractions;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Graylog2Grafana.Services
{
    public class SlackNotificationService : INotificationService
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly INotificationSlackService _notificationSlackService;

        public SlackNotificationService(
            ILogger logger,
            IHttpClientFactory httpClientFactory,
            INotificationSlackService notificationSlackService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _notificationSlackService = notificationSlackService;
        }

        public async Task NotifyAsync(
            MonitorSeries currentMonitor, 
            MonitorType anomalyDetected,
            double lastDataInSeries,
            double preLastDataInSeries)
        {
            try
            {
                var slackConfig = await _notificationSlackService.GetAsync();

                if (!slackConfig.Enabled)
                {
                    _logger.Warning($"Caution | Notification 'Slack' is not enabled");
                    return;
                }

                string message = string.Empty;

                switch (currentMonitor.MonitorSource.SourceType)
                {
                    case SourceType.Graylog:
                        {
                            var graylogBaseUrl = currentMonitor.MonitorSource.Source;

                            var graylogUrl = $"{graylogBaseUrl}/search?q={HttpUtility.UrlEncode(currentMonitor.Query)}&rangetype=absolute&from={DateTime.UtcNow.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}&to={DateTime.UtcNow.AddMinutes(10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}";

                            message = anomalyDetected == MonitorType.Downwards
                                ? $":arrow_down_small: downwards spike on *<{graylogUrl}|{currentMonitor.Name}>* ({preLastDataInSeries} => {lastDataInSeries})"
                                : $":arrow_up_small: upwards spike on *<{graylogUrl}|{currentMonitor.Name}>* ({preLastDataInSeries} => {lastDataInSeries})";

                            break;
                        }
                    default:
                        throw new NotImplementedException($"Source type {currentMonitor.MonitorSource.SourceType} not yet implemented");
                }

                StringContent obj = new StringContent(JsonSerializer.Serialize(new
                {
                    channel = slackConfig.Channel,
                    text = message,
                }), Encoding.UTF8, "application/json");

                using (var httpClient = _httpClientFactory.CreateClient("Slack"))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", slackConfig.BearerToken);

                    using (var response = await httpClient.PostAsync($"/api/chat.postMessage", obj))
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Error sending slack notification | Error {ex.Message}");
            }
        }
    }
}
