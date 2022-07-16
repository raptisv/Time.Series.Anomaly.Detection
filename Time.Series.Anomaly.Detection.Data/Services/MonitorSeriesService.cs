﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Services
{
    public class MonitorSeriesService : IMonitorSeriesService
    {
        private readonly ApplicationDbContext _dbContext;

        public MonitorSeriesService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MonitorSeries>> GetAllAsync()
        {
            return await _dbContext.MonitorSeries
                            .Include(x => x.MonitorSource)
                            .ToListAsync();
        }

        public async Task<MonitorSeries> GetByIdAsync(long id)
        {
            return await _dbContext.MonitorSeries
                            .Include(x => x.MonitorSource)
                            .SingleOrDefaultAsync(x => x.ID == id);
        }

        public async Task CreateAsync(MonitorSeries model)
        {
            model.Name = model.Name.Trim();
            model.Query = model.Query.Trim();

            var existing = await GetAllAsync();

            if (existing.Any(x => x.Name.ToLower().Equals(model.Name.ToLower())))
            {
                throw new Exception($"A definition with name '{model.Name}' already exists");
            }

            _dbContext.MonitorSeries.Add(model);

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(MonitorSeries model)
        {
            model.Name = model.Name.Trim();
            model.Query = model.Query.Trim();

            var existing = await GetAllAsync();

            if (existing.Any(x => x.Name.ToLower().Equals(model.Name.ToLower()) && x.ID != model.ID))
            {
                throw new Exception("Name already exists");
            }

            existing.ForEach(x => _dbContext.Entry(x).State = EntityState.Detached);

            _dbContext.Attach(model);
            _dbContext.Entry(model).Property(x => x.Name).IsModified = true;
            _dbContext.Entry(model).Property(x => x.Description).IsModified = true;
            _dbContext.Entry(model).Property(x => x.Query).IsModified = true;
            _dbContext.Entry(model).Property(x => x.MonitorType).IsModified = true;
            _dbContext.Entry(model).Property(x => x.Sensitivity).IsModified = true;
            _dbContext.Entry(model).Property(x => x.UpperLimitToDetect).IsModified = true;
            _dbContext.Entry(model).Property(x => x.LowerLimitToDetect).IsModified = true;
            _dbContext.Entry(model).Property(x => x.MinuteDurationForAnomalyDetection).IsModified = true;
            _dbContext.Entry(model).Property(x => x.DoNotAlertAgainWithinMinutes).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }


        public async Task DeleteDataAsync(long id)
        {
            var data = _dbContext.MonitorSeriesData.Where(u => u.MonitorSeriesID == id);
            _dbContext.MonitorSeriesData.RemoveRange(data);

            var alerts = _dbContext.AnomalyDetectionData.Where(u => u.MonitorSeriesID == id);
            _dbContext.AnomalyDetectionData.RemoveRange(alerts);

            await _dbContext.SaveChangesAsync();
        }


        public async Task UpdateSensitivityAsync(long id, int sensitivity)
        {
            if (sensitivity<1 || sensitivity > 100)
            {
                throw new Exception("Sensitivity acceptable range is fron 1-100");
            }

            var model = await GetByIdAsync(id);

            model.Sensitivity = sensitivity;

            _dbContext.Entry(model).Property(x => x.Sensitivity).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(MonitorSeries model)
        {
            var item = _dbContext.MonitorSeries.Single(x => x.ID == model.ID);

            _dbContext.MonitorSeries.Remove(item);

            await _dbContext.SaveChangesAsync();
        }
    }
}
