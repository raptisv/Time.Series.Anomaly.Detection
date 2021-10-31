using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Graylog2Grafana.Abstractions
{
    public interface INotificationService
    {
        Task NotifyAsync(
            MonitorSeries currentMonitor,
            MonitorType anomalyDetected,
            double lastItemInSeriesValue,
            double preLastItemInSeriesValue);
    }
}
