using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IMonitorSeriesDataService
    {
        Task CreateOrUpdateByTimestampAsync(long monitorSeriesId, DateTime timeStamp, decimal value);

        Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, long monitorSeriesId, DateTime? timeStamp = null);

        Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, List<long> monitorSeriesIDs = null, DateTime? timeStamp = null);

        Task<List<MonitorSeriesData>> GetInRangeAsync(long monitorSeriesID, DateTime timeStampFrom, DateTime timestampTo);

        Task<List<MonitorSeriesData>> GetInRangeAsync(List<long> monitorSeriesIDs, DateTime timeStampFrom, DateTime timestampTo);

        Task PostCountAsync(long monitorPerMinuteID, DateTime timestamp, decimal value);

        Task UpdateCountAsync(long ID, decimal value);

        Task RemoveEntriesOlderThanMinutesAsync(int minutes);
    }
}
