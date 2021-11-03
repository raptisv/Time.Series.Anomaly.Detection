using Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request;
using Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog2Grafana.Abstractions
{
    public class GrafanaSimpleJsonPluginService : IGrafanaSimpleJsonPluginService
    {
        private readonly IMonitorSeriesService _monitorSeriesService;
        private readonly IMonitorSeriesDataService _monitorSeriesDataService;
        private readonly IAnomalyDetectionDataService _anomalyDetectionDataService;

        public GrafanaSimpleJsonPluginService(
            IMonitorSeriesService monitorSeriesService,
            IMonitorSeriesDataService monitorSeriesDataService,
            IAnomalyDetectionDataService anomalyDetectionDataService)
        {
            _monitorSeriesService = monitorSeriesService;
            _monitorSeriesDataService = monitorSeriesDataService;
            _anomalyDetectionDataService = anomalyDetectionDataService;
        }


        public async Task<IEnumerable<string>> SearchAsync(SearchRequest request)
        {
            var monitorSeries = await _monitorSeriesService.GetAllAsync();

            var result = monitorSeries.Select(x => x.Name).ToList();

            result.Add("all (table only)");

            return result;
        }

        public async Task<IEnumerable<object>> QueryAsync(TimeSeriesRequest request)
        {
            var response = new List<object>();

            if (request != null)
            {
                var monitorSeries = await _monitorSeriesService.GetAllAsync();

                foreach (var reqTarget in request.Targets)
                {

                    switch (reqTarget.Type)
                    {
                        case "table":
                            {
                                var monitorSeriesItems = monitorSeries.Where(x => reqTarget.Target.Equals("all (table only)") || x.Name.Equals(reqTarget.Target));

                                var anomaliesWithinRange = await _anomalyDetectionDataService.GetInRangeAsync(monitorSeriesItems.Select(x => x.ID).ToList(), request.Range.From, request.Range.To);

                                response.Add(new TableResponseTargetItem()
                                {
                                    Type = "table",
                                    Columns = new List<TableResponseTargetItem.Column>()
                                        {
                                            new TableResponseTargetItem.Column(){ Type = "time", Text = "Timestamp" },
                                            new TableResponseTargetItem.Column(){ Type = "string", Text = "Time ago" },
                                            new TableResponseTargetItem.Column(){ Type = "string", Text = "Series" },
                                            new TableResponseTargetItem.Column(){ Type = "string", Text = "Comments" }
                                        },
                                    Rows = anomaliesWithinRange.OrderByDescending(x => x.Timestamp).Select(x => new List<object>()
                                        {
                                            Utils.GetUnixTimestampMilliseconds(x.Timestamp),
                                            x.Timestamp.TimeAgo(),
                                            x.MonitorSeries.Name,
                                            (x.MonitorType == MonitorType.Upwards ? "🔺" : "🔻") + " " + x.Comments
                                        }).ToList()
                                });
                                break;
                            }
                        case "timeserie":
                        default:
                            {
                                var monitorSeriesItems = monitorSeries.Where(x => x.Name.Equals(reqTarget.Target));

                                var monitorSeriesData = await GetMonitorSeriesData(monitorSeriesItems, request.Range.From, request.Range.To);

                                if (monitorSeriesData != null)
                                {
                                    response.Add(new TimeSiriesReponseTargetItem()
                                    {
                                        Target = reqTarget.Target,
                                        Datapoints = monitorSeriesData.Select(x => new List<object>()
                                        {
                                            x.Count, Utils.GetUnixTimestampMilliseconds(x.Timestamp)
                                        }).ToList()
                                    });
                                }
                                break;
                            }
                    }
                }
            }

            return response;
        }

        private async Task<List<MonitorSeriesData>> GetMonitorSeriesData(IEnumerable<MonitorSeries> monitorSeriesItems, DateTime from, DateTime to)
        {
            var monitorSeriesData = (await _monitorSeriesDataService.GetInRangeAsync(monitorSeriesItems.Select(x => x.ID).ToList(), from, to))
                    .OrderBy(x => x.Timestamp)
                    .ToList();

            var currentMinute = Utils.TruncateToMinute(DateTime.UtcNow);

            // Remove current minute as it is probably still in progress of gathering data
            if (monitorSeriesData.RemoveAll(x => Utils.TruncateToMinute(x.Timestamp) == currentMinute) == 0)
            {
                // If current minute did not exist in resultset, remove previous minute as this is probably still in progress of gathering data
                monitorSeriesData.RemoveAll(x => Utils.TruncateToMinute(x.Timestamp) == currentMinute.AddMinutes(-1));
            }

            return monitorSeriesData;
        }

        public async Task<IEnumerable<AnnotationsResponse>> AnnotationsAsync(AnnotationsRequest request)
        {
            var annotationTypes = Enum.GetValues(typeof(MonitorType)).Cast<MonitorType>();

            var response = new List<AnnotationsResponse>();

            var queryItems = (request.Annotation.Query ?? string.Empty).Split('#', StringSplitOptions.RemoveEmptyEntries);

            if (queryItems.Length > 0 && request?.Range != null)
            {
                var annotationTypeName = queryItems[0];

                if (request.Annotation.Enable &&
                    Enum.IsDefined(typeof(MonitorType), annotationTypeName))
                {
                    var annotationType = annotationTypes.Single(x => x.ToString().Equals(annotationTypeName, StringComparison.OrdinalIgnoreCase));

                    // Get spikes of this type withing range
                    var anomaliesWithinRange = await _anomalyDetectionDataService.GetInRangeAsync(annotationType, request.Range.From, request.Range.To);

                    foreach (var item in anomaliesWithinRange)
                    {
                        if (queryItems.Any(x => x.Equals("*") || x.Trim().Equals(item.MonitorSeries.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            // If this series monitor is set for this type
                            if (item.MonitorSeries.MonitorType == MonitorType.DownwardsAndUpwards || item.MonitorSeries.MonitorType == annotationType)
                            {
                                response.Add(new AnnotationsResponse()
                                {
                                    Annotation = request.Annotation,
                                    Title = item.MonitorSeries.Name,
                                    Text = item.Comments,
                                    Time = Utils.GetUnixTimestampMilliseconds(item.Timestamp)
                                });
                            }
                        }
                    }
                }
            }

            return response;
        }
    }
}
