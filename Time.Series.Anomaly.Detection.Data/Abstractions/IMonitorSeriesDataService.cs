using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IMonitorSeriesDataService
    {
        Task CreateOrUpdateByTimestampAsync(long monitorSeriesId, DateTime timeStamp, int count);

        Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, long monitorSeriesId, DateTime? timeStamp = null);

        Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, List<long> monitorSeriesIDs = null, DateTime? timeStamp = null);

        Task<List<MonitorSeriesData>> GetInRangeAsync(long monitorSeriesID, DateTime timeStampFrom, DateTime timestampTo);

        Task<List<MonitorSeriesData>> GetInRangeAsync(List<long> monitorSeriesIDs, DateTime timeStampFrom, DateTime timestampTo);

        Task PostCountAsync(long monitorPerMinuteID, DateTime timestamp, int count);

        Task UpdateCountAsync(long ID, int count);

        Task RemoveEntriesOlderThanMinutesAsync(int minutes);
    }
}
