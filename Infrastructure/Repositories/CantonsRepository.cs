using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class CantonsRepository : ServiceAwareEfRepository<Cantons, string>, ICantonsRepository
{
    public CantonsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Cantons>> GetByProvinceIdAsync(string provinceId)
    {
        return await _dbSet.Where(c => c.ProvinceId == provinceId).ToListAsync();
    }
}
