using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class ProvincesRepository : ServiceAwareEfRepository<Provinces, string>, IProvincesRepository
{
    public ProvincesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Provinces>> GetByCountryIdAsync(string countryId)
    {
        return await _dbSet.Where(p => p.CountryId == countryId).ToListAsync();
    }
}
