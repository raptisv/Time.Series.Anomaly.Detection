using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Time.Series.Anomaly.Detection.Data.Services
{
    public class UsersService : IUsersService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public UsersService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<List<IdentityUser>> GetAllAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.Users.ToListAsync();
        }

        public async Task<IdentityUser> GetByIdAsync(string id)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task DeleteAsync(string id)
        {
            using var scope = _scopeFactory.CreateScope();
            using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var item = _dbContext.Users.Single(x => x.Id == id);

            _dbContext.Users.Remove(item);

            await _dbContext.SaveChangesAsync();
        }
    }
}
