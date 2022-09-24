using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IMonitorSeriesDataService
    {
        Task CreateOrUpdateByTimestampAsync(long monitorSeriesId, string groupValue, DateTime timeStamp, decimal value);

        Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, long monitorSeriesId, string groupValue, DateTime? timeStamp = null);

        Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, List<long> monitorSeriesIDs, string groupValue, DateTime? timeStamp = null);

        Task<List<MonitorSeriesData>> GetInRangeAsync(long monitorSeriesID, string groupValue, DateTime timeStampFrom, DateTime timestampTo);

        Task<List<MonitorSeriesData>> GetInRangeAsync(List<long> monitorSeriesIDs, string groupValue, DateTime timeStampFrom, DateTime timestampTo);

        Task PostCountAsync(long monitorPerMinuteID, string groupValue, DateTime timestamp, decimal value);

        Task UpdateCountAsync(long ID, decimal value);

        Task RemoveEntriesOlderThanMinutesAsync(int minutes);
    }
}
