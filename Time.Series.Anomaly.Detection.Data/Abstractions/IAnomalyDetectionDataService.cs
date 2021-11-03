using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IAnomalyDetectionDataService
    {
        Task<bool> CreateIfNotAlreadyExistsAsync(long monitorSeriesID, DateTime timestamp, MonitorType type, string comments);
        Task<bool> ExistsByMonitorSeriesAndTimestampAsync(long monitorSeriesId, DateTime timestamp);
        Task<List<AnomalyDetectionData>> GetInRangeAsync(List<long> monitorSeriesIds, DateTime from, DateTime to);
        Task<List<AnomalyDetectionData>> GetInRangeAsync(MonitorType type, DateTime from, DateTime to);
        Task<AnomalyDetectionData> GetLatestForSeriesAsync(long monitorSeriesId);
    }
}
