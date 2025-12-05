using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class CantonsRepository : ServiceAwareEfRepository<Cantons, string>, ICantonsRepository
{

    private readonly DbContext _db;
    public CantonsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;

    }

    public async Task<IEnumerable<Cantons>> GetByProvinceIdAsync(string provinceId)
    {
        return await _db.Set<Cantons>().Where(c => c.ProvinceId == provinceId).ToListAsync();
    }
}
