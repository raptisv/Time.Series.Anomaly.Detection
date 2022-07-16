using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Services
{
    public class MonitorSourcesService : IMonitorSourcesService
    {
        private readonly ApplicationDbContext _dbContext;

        public MonitorSourcesService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MonitorSources>> GetAllAsync()
        {
            return await _dbContext.MonitorSources.ToListAsync();
        }

        public async Task<MonitorSources> GetByIdAsync(long id)
        {
            return await _dbContext.MonitorSources.SingleOrDefaultAsync(x => x.ID == id);
        }

        public async Task CreateAsync(MonitorSources model)
        {
            model.Name = model.Name.Trim();

            var existing = await GetAllAsync();

            if (existing.Any(x => x.Name.ToLower().Equals(model.Name.ToLower())))
            {
                throw new Exception($"A definition with name '{model.Name}' already exists");
            }

            _dbContext.MonitorSources.Add(model);

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(MonitorSources model)
        {
            model.Name = model.Name.Trim();

            var existing = await GetAllAsync();

            if (existing.Any(x => x.Name.ToLower().Equals(model.Name.ToLower()) && x.ID != model.ID))
            {
                throw new Exception("Name already exists");
            }

            existing.ForEach(x => _dbContext.Entry(x).State = EntityState.Detached);

            _dbContext.Attach(model);
            _dbContext.Entry(model).Property(x => x.Name).IsModified = true;
            _dbContext.Entry(model).Property(x => x.SourceType).IsModified = true;
            _dbContext.Entry(model).Property(x => x.Source).IsModified = true;
            _dbContext.Entry(model).Property(x => x.Username).IsModified = true;
            _dbContext.Entry(model).Property(x => x.Password).IsModified = true;
            _dbContext.Entry(model).Property(x => x.LoadDataIntervalSeconds).IsModified = true;
            _dbContext.Entry(model).Property(x => x.DataRetentionInMinutes).IsModified = true;
            _dbContext.Entry(model).Property(x => x.DetectionDelayInMinutes).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateLastTimestampAsync(long id, DateTime timestamp)
        {
            var model = await GetByIdAsync(id);

            model.LastTimestamp = timestamp;

            _dbContext.Entry(model).Property(x => x.LastTimestamp).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }


        public async Task DeleteAsync(MonitorSources model)
        {
            var item = _dbContext.MonitorSources.Single(x => x.ID == model.ID);

            _dbContext.MonitorSources.Remove(item);

            await _dbContext.SaveChangesAsync();
        }
    }
}
