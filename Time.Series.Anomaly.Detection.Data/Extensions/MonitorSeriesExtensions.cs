using System.Collections.Generic;
using System.Linq;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Extensions
{
    public static class MonitorSeriesExtensions
    {
        public static IEnumerable<string> GroupByValues_List(this MonitorSeries monitorSeries)
        {
            return monitorSeries.GroupByValues?.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
                     ?? Enumerable.Empty<string>();
        }

        // All monitor series derived from grouping

        public static IEnumerable<MonitorSeriesForGroupValue> AllWithGrouping(this MonitorSeries monitorSeries)
        {
            if (string.IsNullOrWhiteSpace(monitorSeries.GroupBy) || !monitorSeries.GroupByValues.Any())
            {
                return new List<MonitorSeriesForGroupValue>() {
                    new MonitorSeriesForGroupValue()
                    {
                        ID = monitorSeries.ID,
                        Name = monitorSeries.Name,
                        Query = monitorSeries.Query,
                        Description = monitorSeries.Description,
                        LowerLimitToDetect = monitorSeries.LowerLimitToDetect,
                        UpperLimitToDetect = monitorSeries.UpperLimitToDetect,
                        DoNotAlertAgainWithinMinutes = monitorSeries.DoNotAlertAgainWithinMinutes,
                        MinuteDurationForAnomalyDetection = monitorSeries.MinuteDurationForAnomalyDetection,
                        MonitorSourceID = monitorSeries.MonitorSourceID,
                        MonitorType = monitorSeries.MonitorType,
                        Sensitivity = monitorSeries.Sensitivity,
                        Aggregation = monitorSeries.Aggregation,
                        Field = monitorSeries.Field,
                        GroupBy = monitorSeries.GroupBy,
                        GroupByValues = monitorSeries.GroupByValues,
                        AnomalyDetectionData = monitorSeries.AnomalyDetectionData,
                        MonitorSeriesData = monitorSeries.MonitorSeriesData,
                        MonitorSource = monitorSeries.MonitorSource,
                        GroupValue = null
                    }
                };
            }

            return monitorSeries.GroupByValues_List().Select(x => 
            {
                // IMPORTANT! Do not change the name if not for good reason. This is used for Grafana search.
                var name = $"{monitorSeries.Name} ({monitorSeries.GroupBy.Trim()}:\"{x}\")";

                return new MonitorSeriesForGroupValue()
                {
                    ID = monitorSeries.ID,
                    Name = name,
                    Query = $"{monitorSeries.Query} AND {monitorSeries.GroupBy.Trim()}:\"{x}\"",
                    Description = $"{monitorSeries.Description} for {monitorSeries.GroupBy.Trim()}:{x}",
                    LowerLimitToDetect = monitorSeries.LowerLimitToDetect,
                    UpperLimitToDetect = monitorSeries.UpperLimitToDetect,
                    DoNotAlertAgainWithinMinutes = monitorSeries.DoNotAlertAgainWithinMinutes,
                    MinuteDurationForAnomalyDetection = monitorSeries.MinuteDurationForAnomalyDetection,
                    MonitorSourceID = monitorSeries.MonitorSourceID,
                    MonitorType = monitorSeries.MonitorType,
                    Sensitivity = monitorSeries.Sensitivity,
                    Aggregation = monitorSeries.Aggregation,
                    Field = monitorSeries.Field,
                    GroupBy = monitorSeries.GroupBy,
                    GroupByValues = monitorSeries.GroupByValues,
                    AnomalyDetectionData = monitorSeries.AnomalyDetectionData,
                    MonitorSeriesData = monitorSeries.MonitorSeriesData,
                    MonitorSource = monitorSeries.MonitorSource,
                    GroupValue = x
                };
            });
        }
    }
}
