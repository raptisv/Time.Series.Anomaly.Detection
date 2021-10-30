using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Graylog.Grafana.Services
{
    public class SlackNotificationService : INotificationService
    {
        private readonly ILogger _logger;
        private readonly IOptions<SlackConfiguration> _slackConfiguration;
        private readonly HttpClient _httpClient;

        public SlackNotificationService(
            ILogger logger,
            IOptions<SlackConfiguration> slackConfiguration,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _slackConfiguration = slackConfiguration;
            _httpClient = clientFactory.CreateClient("Slack");
        }

        public async Task NotifyAsync(string message)
        {
            try
            {
                if (!_slackConfiguration.Value.Enabled)
                {
                    return;
                }

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
