using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class ProvincesRepository : ServiceAwareEfRepository<Provinces, string>, IProvincesRepository
{
    private readonly DbContext _db;
    public ProvincesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<Provinces>> GetByCountryIdAsync(string countryId)
    {
        return await _db.Set<Provinces>().Where(p => p.CountryId == countryId).ToListAsync();
    }
}
