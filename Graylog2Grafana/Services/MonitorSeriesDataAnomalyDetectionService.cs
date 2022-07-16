using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Time.Series.Anomaly.Detection.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog2Grafana.Services
{
    public class MonitorSeriesDataAnomalyDetectionService : IMonitorSeriesDataAnomalyDetectionService
    {
        private readonly IAnomalyDetectionService _anomalyDetectionService;

        public MonitorSeriesDataAnomalyDetectionService(
            IAnomalyDetectionService anomalyDetectionService)
        {
            _anomalyDetectionService = anomalyDetectionService;
        }

        public DataAnomalyDetectionResult DetectAnomalies(
              MonitorSeries monitorSeries,
              List<MonitorSeriesData> monitorSeriesData)
        {
            // Remove current minute from the calculations as it is probably still in progress of gathering data
            monitorSeriesData.RemoveAll(x => Utils.TruncateToMinute(x.Timestamp) == Utils.TruncateToMinute(DateTime.UtcNow));

            // Remove some minutes in case the datasource needs some time to gather data
            var dateFromToIgnoreValues = Utils.TruncateToMinute(DateTime.UtcNow.AddMinutes(-Math.Abs(monitorSeries.MonitorSource.DetectionDelayInMinutes)));
            monitorSeriesData.RemoveAll(x => Utils.TruncateToMinute(x.Timestamp) >= dateFromToIgnoreValues);

            if (monitorSeriesData.Count < 12)
            {
                return null;
            }

            var rowData = monitorSeriesData
                .Select(x => new RowDataItem() { Timestamp = x.Timestamp, Count = x.Count })
                .OrderBy(x => x.Timestamp);

            var result = _anomalyDetectionService.Detect(rowData, monitorSeries.Sensitivity);

            // If the last minute is an anomaly, persist & notify
            result.Series.Reverse();
            var last = result.Series.First();
            var preLast = result.Series.Skip(1).Take(1).First();
            var isUpwardsSpike = last.Data > preLast.Data;
            var isDownwardsSpike = last.Data < preLast.Data;

            var isInAcceptedLowerLimit = true;
            if (monitorSeries.LowerLimitToDetect.HasValue)
            {
                isInAcceptedLowerLimit = preLast.Data > monitorSeries.LowerLimitToDetect.Value && last.Data > monitorSeries.LowerLimitToDetect.Value;
            }

            var isInAcceptedUpperLimit = true;
            if (monitorSeries.UpperLimitToDetect.HasValue)
            {
                isInAcceptedUpperLimit = preLast.Data < monitorSeries.UpperLimitToDetect.Value && last.Data < monitorSeries.UpperLimitToDetect.Value;
            }

            var anomalyDetectedAtLatestTimeStamp =
                last.IsAnomaly &&
                last.Data != preLast.Data &&
                isInAcceptedLowerLimit && // Do not bother for very small values
                isInAcceptedUpperLimit && // Do not bother for very large values
                ((monitorSeries.MonitorType == MonitorType.DownwardsAndUpwards) ||
                (monitorSeries.MonitorType == MonitorType.Upwards && isUpwardsSpike) ||
                (monitorSeries.MonitorType == MonitorType.Downwards && isDownwardsSpike));

            return new DataAnomalyDetectionResult(
                monitorSeries,
                result.Series,
                anomalyDetectedAtLatestTimeStamp,
                isUpwardsSpike ? MonitorType.Upwards : MonitorType.Downwards,
                last.TimeStamp,
                last.Data,
                preLast.Data);
        }
    }
}
