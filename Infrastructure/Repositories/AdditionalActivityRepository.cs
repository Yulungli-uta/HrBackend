using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class AdditionalActivityRepository : ServiceAwareEfRepository<AdditionalActivity, int>, IAdditionalActivityRepository
{
    private readonly DbContext _db;

    public AdditionalActivityRepository(WsUtaSystem.Data.AppDbContext db) : base(db) 
    {
        _db = db;
    }

    public async Task<IEnumerable<AdditionalActivity>> GetByContractIDAsync(int ContractId, CancellationToken ct)
    {
        return await _db.Set<AdditionalActivity>()
                .Where(rt => rt.ContractId == ContractId && rt.IsActive)
                .ToListAsync(ct);
    }

}
