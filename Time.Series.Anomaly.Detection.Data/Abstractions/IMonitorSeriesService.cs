using System.Collections.Generic;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IMonitorSeriesService
    {
        Task<List<MonitorSeries>> GetAllAsync();
        Task<MonitorSeries> GetByIdAsync(long id);
        Task CreateAsync(MonitorSeries item);
        Task UpdateAsync(MonitorSeries item);
        Task DeleteAsync(MonitorSeries item);
    }
}
