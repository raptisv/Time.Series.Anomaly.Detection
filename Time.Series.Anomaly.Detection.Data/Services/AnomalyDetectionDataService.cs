using Microsoft.EntityFrameworkCore;
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
        private readonly ApplicationDbContext _dbContext;

        public AnomalyDetectionDataService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CreateIfNotAlreadyExistsAsync(long monitorSeriesID, DateTime timeStamp, MonitorType type, string comments)
        {
            var sameAlertExists = await ExistsByMonitorSeriesAndTimestampAsync(monitorSeriesID, timeStamp);

            if (!sameAlertExists)
            {
                _dbContext.AnomalyDetectionData.Add(new AnomalyDetectionData()
                {
                    ID = 0,
                    Timestamp = timeStamp,
                    MonitorSeriesID = monitorSeriesID,
                    Comments = comments,
                    MonitorType = type
                });

                await _dbContext.SaveChangesAsync();

                return true;
            }

            return false;
        }

        public async Task<bool> ExistsByMonitorSeriesAndTimestampAsync(long monitorSeriesID, DateTime timestamp)
        {
            return (await _dbContext.AnomalyDetectionData
                            .Where(x => x.MonitorSeriesID == monitorSeriesID && x.Timestamp == timestamp)
                            .ToListAsync())
                            .Any();
        }

        public async Task<List<AnomalyDetectionData>> GetInRangeAsync(List<long> monitorSeriesIds, DateTime from, DateTime to)
        {
            return await _dbContext.AnomalyDetectionData
                            .Include(x => x.MonitorSeries)
                            .Where(x => monitorSeriesIds.Contains(x.MonitorSeriesID) && x.Timestamp >= from && x.Timestamp <= to)
                            .ToListAsync();
        }

        public async Task<List<AnomalyDetectionData>> GetInRangeAsync(MonitorType type, DateTime from, DateTime to)
        {
            return await _dbContext.AnomalyDetectionData
                            .Include(x => x.MonitorSeries)
                            .Where(x => x.MonitorType == type && x.Timestamp >= from && x.Timestamp <= to)
                            .ToListAsync();
        }

        public async Task<AnomalyDetectionData> GetLatestForSeriesAsync(long monitorSeriesId)
        {
            return await _dbContext.AnomalyDetectionData
                            .Where(x => x.MonitorSeriesID == monitorSeriesId)
                            .OrderByDescending(x => x.Timestamp)
                            .FirstOrDefaultAsync();
        }
    }
}
