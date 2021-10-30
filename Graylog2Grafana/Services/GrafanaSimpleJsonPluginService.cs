using Graylog.Grafana.Models.SimpleJsonGrafanaPlugin.Request;
using Graylog.Grafana.Models.SimpleJsonGrafanaPlugin.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
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

            return monitorSeries.Select(x => x.Name).ToList();
        }

        public async Task<IEnumerable<TimeSiriesReponseTargetItem>> QueryAsync(TimeSeriesRequest request)
        {
            var response = new List<TimeSiriesReponseTargetItem>();

            if (request != null)
            {
                var monitorSeries = await _monitorSeriesService.GetAllAsync();

                foreach (var reqTarget in request.Targets)
                {
                    var monitorSeriesItem = monitorSeries.SingleOrDefault(x => x.Name.Equals(reqTarget.Target));

                    if (monitorSeriesItem != null)
                    {
                        var monitorSeriesData = await _monitorSeriesDataService.GetInRangeAsync(monitorSeriesItem.ID, request.Range.From, request.Range.To);

                        // Remove current minute from the calculations as it is probably still in progress of gathering data
                        monitorSeriesData.RemoveAll(x => Utils.TruncateToMinute(x.Timestamp) == Utils.TruncateToMinute(DateTime.UtcNow));

                        response.Add(new TimeSiriesReponseTargetItem()
                        {
                            Target = reqTarget.Target,
                            Datapoints = monitorSeriesData.OrderBy(x => x.Timestamp).Select(x => new List<object>()
                            {
                                x.Count, Utils.GetUnixTimestampMilliseconds(x.Timestamp)
                            }).ToList()
                        });
                    }
                }
            }

            return response;
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
                        if (queryItems.Any(x => x.Trim().Equals(item.MonitorSeries.Name, StringComparison.OrdinalIgnoreCase)))
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
