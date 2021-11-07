using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Time.Series.Anomaly.Detection.Data.Abstractions
{
    public interface IUsersService
    {
        Task DeleteAsync(string id);
        Task<List<IdentityUser>> GetAllAsync();
        Task<IdentityUser> GetByIdAsync(string id);
    }
}
