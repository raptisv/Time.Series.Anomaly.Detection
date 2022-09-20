using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Services
{
    public class MonitorSeriesDataService : IMonitorSeriesDataService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public MonitorSeriesDataService( IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task CreateOrUpdateByTimestampAsync(long monitorSeriesId, DateTime timeStamp, decimal value)
        {
            var monitorPerMinuteDataResult = (await GetLatestAsync(1, monitorSeriesId, timeStamp)).SingleOrDefault();

            if (monitorPerMinuteDataResult != null)
            {
                await UpdateCountAsync(monitorPerMinuteDataResult.ID, value);
            }
            else
            {
                await PostCountAsync(monitorSeriesId, timeStamp, value);
            }
        }

        public Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, long monitorSeriesId, DateTime? timeStamp = null)
        {
            return GetLatestAsync(pageSize, new List<long>() { monitorSeriesId }, timeStamp);
        }

        public async Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, List<long> monitorSeriesIDs, DateTime? timeStamp = null)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            List<MonitorSeriesData> result = null;

            if (timeStamp.HasValue)
            {
                result = await _dbContext.MonitorSeriesData
                    .Where(x => monitorSeriesIDs.Contains(x.MonitorSeriesID) && x.Timestamp == timeStamp.Value)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                result = await _dbContext.MonitorSeriesData
                    .Where(x => monitorSeriesIDs.Contains(x.MonitorSeriesID))
                    .OrderByDescending(x => x.Timestamp)
                    .Take(pageSize)
                    .ToListAsync();
            }

            return result;
        }

        public Task<List<MonitorSeriesData>> GetInRangeAsync(long monitorSeriesID, DateTime timeStampFrom, DateTime timestampTo)
        {
            return GetInRangeAsync(new List<long>() { monitorSeriesID }, timeStampFrom, timestampTo);
        }

        public async Task<List<MonitorSeriesData>> GetInRangeAsync(List<long> monitorSeriesIDs, DateTime timeStampFrom, DateTime timestampTo)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.MonitorSeriesData
                            .Where(x => monitorSeriesIDs.Contains(x.MonitorSeriesID) && x.Timestamp >= timeStampFrom && x.Timestamp <= timestampTo)
                            .OrderByDescending(x => x.Timestamp)
                            .Take(1000)
                            .ToListAsync();
        }

        public async Task PostCountAsync(long monitorSeriesID, DateTime timestamp, decimal value)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _dbContext.MonitorSeriesData.Add(new MonitorSeriesData()
            {
                ID = 0,
                Timestamp = timestamp,
                MonitorSeriesID = monitorSeriesID,
                Value = value
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateCountAsync(long ID, decimal value)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var item = _dbContext.MonitorSeriesData.Single(x => x.ID == ID);
            item.Value = value;
            _dbContext.Attach(item);
            _dbContext.Entry(item).Property(x => x.Value).IsModified = true;
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveEntriesOlderThanMinutesAsync(int minutes)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var oldDate = DateTime.UtcNow.AddMinutes(-Math.Abs(minutes));
            var oldItems = _dbContext.MonitorSeriesData.Where(u => u.Timestamp < oldDate);
            _dbContext.MonitorSeriesData.RemoveRange(oldItems);
            await _dbContext.SaveChangesAsync();
        }
    }
}
