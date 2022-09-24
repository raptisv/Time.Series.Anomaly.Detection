using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Models.Enums;

namespace Time.Series.Anomaly.Detection.Data.Services
{
    public class AnomalyDetectionDataService : IAnomalyDetectionDataService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AnomalyDetectionDataService( IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> CreateIfNotAlreadyExistsAsync(long monitorSeriesID, string groupValue, DateTime timeStamp, MonitorType type, string comments)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var sameAlertExists = await ExistsByMonitorSeriesAndTimestampAsync(monitorSeriesID, groupValue, timeStamp);

            if (!sameAlertExists)
            {
                _dbContext.AnomalyDetectionData.Add(new AnomalyDetectionData()
                {
                    ID = 0,
                    Timestamp = timeStamp,
                    MonitorSeriesID = monitorSeriesID,
                    MonitorSeriesGroupValue = groupValue,
                    Comments = comments,
                    MonitorType = type
                });

                await _dbContext.SaveChangesAsync();

                return true;
            }

            return false;
        }

        private async Task<bool> ExistsByMonitorSeriesAndTimestampAsync(long monitorSeriesID, string groupValue, DateTime timestamp)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return (await _dbContext.AnomalyDetectionData
                            .Where(x => x.MonitorSeriesID == monitorSeriesID && x.MonitorSeriesGroupValue == groupValue && x.Timestamp == timestamp)
                            .ToListAsync())
                            .Any();
        }

        public async Task<List<AnomalyDetectionData>> GetInRangeAsync(List<long> monitorSeriesIds, DateTime from, DateTime to)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.AnomalyDetectionData
                            .Include(x => x.MonitorSeries)
                            .Where(x => monitorSeriesIds.Contains(x.MonitorSeriesID) && x.Timestamp >= from && x.Timestamp <= to)
                            .ToListAsync();
        }

        public async Task<List<AnomalyDetectionData>> GetInRangeAsync(MonitorType type, DateTime from, DateTime to)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.AnomalyDetectionData
                            .Include(x => x.MonitorSeries)
                            .Where(x => x.MonitorType == type && x.Timestamp >= from && x.Timestamp <= to)
                            .ToListAsync();
        }

        public async Task<AnomalyDetectionData> GetLatestForSeriesAsync(long monitorSeriesId, string groupValue)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.AnomalyDetectionData
                            .Where(x => x.MonitorSeriesID == monitorSeriesId && x.MonitorSeriesGroupValue == groupValue)
                            .OrderByDescending(x => x.Timestamp)
                            .FirstOrDefaultAsync();
        }
    }
}
