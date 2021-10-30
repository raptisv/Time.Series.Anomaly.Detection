using System.Threading.Tasks;

namespace Graylog2Grafana.Abstractions
{
    public interface IDataService
    {
        Task LoadDataAsync();
    }
}
