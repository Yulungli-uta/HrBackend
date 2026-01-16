using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class ParametersRepository : ServiceAwareEfRepository<Parameters, int>, IParametersRepository
{
    private readonly DbContext _db;
    public ParametersRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<Parameters>> GetByNameAsync(string name, CancellationToken ct)
    {
        return await _db.Set<Parameters>()
                .Where(p => p.Name == name && p.IsActive)
                .ToListAsync(ct);
    }
}

