using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IMonitorSourcesService
    {
        Task<List<MonitorSources>> GetAllAsync();
        Task<MonitorSources> GetByIdAsync(long id);
        Task CreateAsync(MonitorSources item);
        Task UpdateAsync(MonitorSources item);
        Task DeleteAsync(MonitorSources item);
        Task UpdateLastTimestampAsync(long id, DateTime timestamp);
    }
}
