using Graylog.Grafana.Models.Graylog;
using Graylog2Grafana.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog.Grafana.Services
{
    public class GraylogDataService : IDataService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly HttpClient _httpClient;

        public GraylogDataService(
            IServiceProvider serviceProvider,
            IHttpClientFactory clientFactory)
        {
            _serviceProvider = serviceProvider;
            _httpClient = clientFactory.CreateClient("Graylog");
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
    }
}
