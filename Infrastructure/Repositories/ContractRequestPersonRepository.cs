using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class ContractRequestPersonRepository
    : ServiceAwareEfRepository<ContractRequestPerson, int>, IContractRequestPersonRepository
{
    public ContractRequestPersonRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<ContractRequestPerson>> GetByRequestAsync(int requestId, CancellationToken ct = default)
        => await _db.Set<ContractRequestPerson>()
            .AsNoTracking()
            .Include(p => p.Person)
            .Include(p => p.Job)
            .Where(p => p.RequestId == requestId)
            .OrderBy(p => p.RequestPersonId)
            .ToListAsync(ct);

    public async Task<IEnumerable<ContractRequestPerson>> GetPendingByRequestAsync(
        int requestId, int pendingStatusId, CancellationToken ct = default)
        => await _db.Set<ContractRequestPerson>()
            .AsNoTracking()
            .Include(p => p.Person)
            .Include(p => p.Job)
            .Where(p => p.RequestId == requestId && p.StatusId == pendingStatusId)
            .OrderBy(p => p.RequestPersonId)
            .ToListAsync(ct);
}
