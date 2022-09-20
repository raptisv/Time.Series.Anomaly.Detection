using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Notifications;

namespace Time.Series.Anomaly.Detection.Data.Services
{
    public class NotificationSlackService : INotificationSlackService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationSlackService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<NotificationSlack> GetAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var model = await _dbContext.SlackNotification.FirstOrDefaultAsync();

            if (model is null)
            {
                model = new NotificationSlack()
                {
                    ID = 1,
                    Enabled = false,
                    Channel = string.Empty,
                    BearerToken = string.Empty
                };

                _dbContext.SlackNotification.Add(model);

                await _dbContext.SaveChangesAsync();
            }

            return model;
        }

        public async Task UpdateAsync(NotificationSlack model)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _dbContext.Attach(model);
            _dbContext.Entry(model).Property(x => x.Enabled).IsModified = true;
            _dbContext.Entry(model).Property(x => x.Channel).IsModified = true;
            _dbContext.Entry(model).Property(x => x.BearerToken).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }
    }
}
