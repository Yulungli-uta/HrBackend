using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class ContractRequestRepository : ServiceAwareEfRepository<ContractRequest, int>, IContractRequestRepository
{
    public ContractRequestRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<ContractRequest>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        => await _db.Set<ContractRequest>()
            .AsNoTracking()
            .Where(x => x.Status == statusTypeId)
            .OrderByDescending(x => x.RequestId)
            .ToListAsync(ct);

    public async Task IncrementTotalHiredAsync(int requestId, CancellationToken ct = default)
    {
        var entity = await _db.Set<ContractRequest>()
            .FirstOrDefaultAsync(x => x.RequestId == requestId, ct)
            ?? throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

        entity.TotalPeopleHired++;
        entity.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(int requestId, int statusTypeId, CancellationToken ct = default)
    {
        var entity = await _db.Set<ContractRequest>()
            .FirstOrDefaultAsync(x => x.RequestId == requestId, ct)
            ?? throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

        entity.Status = statusTypeId;
        entity.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
    }
}
