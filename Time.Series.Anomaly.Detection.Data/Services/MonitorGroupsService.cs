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
    public class MonitorGroupsService : IMonitorGroupsService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public MonitorGroupsService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<List<MonitorGroups>> GetAllAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.MonitorGroups.ToListAsync();
        }

        public async Task<MonitorGroups> GetByIdAsync(long id)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.MonitorGroups.SingleOrDefaultAsync(x => x.ID == id);
        }

        public async Task CreateAsync(MonitorGroups model)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            model.Name = model.Name.Trim();

            var existing = await GetAllAsync();

            if (existing.Any(x => x.Name.ToLower().Equals(model.Name.ToLower())))
            {
                throw new Exception($"A group with name '{model.Name}' already exists");
            }

            _dbContext.MonitorGroups.Add(model);

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(MonitorGroups model)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            model.Name = model.Name.Trim();

            var existing = await GetAllAsync();

            if (existing.Any(x => x.Name.ToLower().Equals(model.Name.ToLower()) && x.ID != model.ID))
            {
                throw new Exception("Name already exists");
            }

            existing.ForEach(x => _dbContext.Entry(x).State = EntityState.Detached);

            _dbContext.Attach(model);
            _dbContext.Entry(model).Property(x => x.Name).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(MonitorGroups model)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var item = _dbContext.MonitorGroups.Single(x => x.ID == model.ID);

            _dbContext.MonitorGroups.Remove(item);

            await _dbContext.SaveChangesAsync();
        }
    }
}
