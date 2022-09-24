using System.Collections.Generic;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IMonitorGroupsService
    {
        Task<List<MonitorGroups>> GetAllAsync();
        Task<MonitorGroups> GetByIdAsync(long id);
        Task CreateAsync(MonitorGroups item);
        Task UpdateAsync(MonitorGroups item);
        Task DeleteAsync(MonitorGroups item);
    }
}
