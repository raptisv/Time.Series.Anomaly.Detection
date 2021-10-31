using Microsoft.EntityFrameworkCore;
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
        private readonly ApplicationDbContext _dbContext;

        public MonitorSeriesDataService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateOrUpdateByTimestampAsync(long monitorSeriesId, DateTime timeStamp, int count)
        {
            var monitorPerMinuteDataResult = (await GetLatestAsync(1, monitorSeriesId, timeStamp)).SingleOrDefault();

            if (monitorPerMinuteDataResult != null)
            {
                await UpdateCountAsync(monitorPerMinuteDataResult.ID, count);
            }
            else
            {
                await PostCountAsync(monitorSeriesId, timeStamp, count);
            }
        }

        public Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, long monitorSeriesId, DateTime? timeStamp = null)
        {
            return GetLatestAsync(pageSize, new List<long>() { monitorSeriesId }, timeStamp);
        }

        public async Task<List<MonitorSeriesData>> GetLatestAsync(int pageSize, List<long> monitorSeriesIDs, DateTime? timeStamp = null)
        {
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
            return await _dbContext.MonitorSeriesData
                            .Where(x => monitorSeriesIDs.Contains(x.MonitorSeriesID) && x.Timestamp >= timeStampFrom && x.Timestamp <= timestampTo)
                            .OrderByDescending(x => x.Timestamp)
                            .Take(1000)
                            .ToListAsync();
        }

        public async Task PostCountAsync(long monitorSeriesID, DateTime timestamp, int count)
        {
            _dbContext.MonitorSeriesData.Add(new MonitorSeriesData()
            {
                ID = 0,
                Timestamp = timestamp,
                MonitorSeriesID = monitorSeriesID,
                Count = count
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateCountAsync(long ID, int count)
        {
            var item = _dbContext.MonitorSeriesData.Single(x => x.ID == ID);
            item.Count = count;
            _dbContext.Attach(item);
            _dbContext.Entry(item).Property(x => x.Count).IsModified = true;
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveEntriesOlderThanAsync(uint days)
        {
            var oldDate = DateTime.Now.AddDays(-days);
            var oldItems = _dbContext.MonitorSeriesData.Where(u => u.Timestamp < oldDate);
            _dbContext.MonitorSeriesData.RemoveRange(oldItems);
            await _dbContext.SaveChangesAsync();
        }
    }
}
