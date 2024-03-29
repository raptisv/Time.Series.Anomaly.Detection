﻿using Graylog2Grafana.Models;
using System.Collections.Generic;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Graylog2Grafana.Abstractions
{
    public interface IMonitorSeriesDataAnomalyDetectionService
    {
        DataAnomalyDetectionResult DetectAnomalies(MonitorSeries monitorSeries, List<MonitorSeriesData> monitorSeriesData);
    }
}
