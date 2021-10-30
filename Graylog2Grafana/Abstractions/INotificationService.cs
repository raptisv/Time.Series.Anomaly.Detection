using System.Threading.Tasks;

namespace Graylog2Grafana.Abstractions
{
    public interface INotificationService
    {
        Task NotifyAsync(string message);
    }
}
