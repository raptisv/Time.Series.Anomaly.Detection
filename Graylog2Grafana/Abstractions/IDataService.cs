using Graylog2Grafana.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Graylog2Grafana.Abstractions
{
    public interface IDataService
    {
        Task LoadDataAsync();
        Task<List<DataAnomalyDetectionResult>> DetectAndPersistAnomaliesAsync();
    }
}
