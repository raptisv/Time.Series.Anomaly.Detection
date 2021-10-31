using System;
using System.Collections.Generic;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;
using Time.Series.Anomaly.Detection.Models;

namespace Graylog2Grafana.Models
{
    public class DataAnomalyDetectionResult
    {
        public MonitorSeries MonitorSeries;
        public List<PredictionResult> PredictionResult;
        public bool AnomalyDetectedAtLatestTimeStamp;
        public MonitorType MonitorType;
        public DateTime TimestampDetected;
        public double LastDataInSeries;
        public double PrelastDataInSeries;

        public DataAnomalyDetectionResult(
            MonitorSeries monitorSeries,
            List<PredictionResult> series,
            bool anomalyDetectedAtLatestTimeStamp,
            MonitorType monitorType,
            DateTime timestampDetected,
            double lastDataInSeries,
            double prelastDataInSeries)
        {
            MonitorSeries = monitorSeries;
            PredictionResult = series;
            AnomalyDetectedAtLatestTimeStamp = anomalyDetectedAtLatestTimeStamp;
            MonitorType = monitorType;
            TimestampDetected = timestampDetected;
            LastDataInSeries = lastDataInSeries;
            PrelastDataInSeries = prelastDataInSeries;
        }
    }
}
