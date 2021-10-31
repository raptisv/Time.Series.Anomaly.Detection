using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Graylog2Grafana.Services
{
    public class SlackNotificationService : INotificationService
    {
        private readonly ILogger _logger;
        private readonly IOptions<GraylogConfiguration> _graylogConfiguration;
        private readonly IOptions<SlackConfiguration> _slackConfiguration;
        private readonly HttpClient _httpClient;

        public SlackNotificationService(
            ILogger logger,
            IOptions<SlackConfiguration> slackConfiguration, 
            IOptions<GraylogConfiguration> graylogConfiguration,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _slackConfiguration = slackConfiguration;
            _graylogConfiguration = graylogConfiguration;
            _httpClient = clientFactory.CreateClient("Slack");
        }

        public async Task NotifyAsync(
            MonitorSeries currentMonitor, 
            MonitorType anomalyDetected,
            double lastDataInSeries,
            double preLastDataInSeries)
        {
            try
            {
                if (!_slackConfiguration.Value.Enabled)
                {
                    return;
                }

                var graylogUrl = $"{_graylogConfiguration.Value.Url}/search?q={HttpUtility.UrlEncode(currentMonitor.Query)}&rangetype=absolute&from={DateTime.UtcNow.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}&to={DateTime.UtcNow.AddMinutes(10).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}";

                var percentage = preLastDataInSeries > 0
                    ? Math.Abs(lastDataInSeries - preLastDataInSeries) / preLastDataInSeries * 100.0
                    : 100;

                var message = anomalyDetected == MonitorType.Downwards
                    ? $":arrow_down_small: {(int)percentage}% downwards spike on *<{graylogUrl}|{currentMonitor.Name}>* ({preLastDataInSeries} => {lastDataInSeries})"
                    : $":arrow_up_small: {(int)percentage}% upwards spike on *<{graylogUrl}|{currentMonitor.Name}>* ({preLastDataInSeries} => {lastDataInSeries})";

                StringContent obj = new StringContent(JsonConvert.SerializeObject(new
                {
                    channel = _slackConfiguration.Value.Channel,
                    text = message,
                }), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync($"/api/chat.postMessage", obj);

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Error sending slack notification | Error {ex.Message}");
            }
        }
    }
}
