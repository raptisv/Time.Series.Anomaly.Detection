using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Models.Notifications;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface INotificationSlackService
    {
        Task<NotificationSlack> GetAsync();
        Task UpdateAsync(NotificationSlack item);
    }
}
