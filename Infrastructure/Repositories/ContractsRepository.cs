using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class ContractsRepository : ServiceAwareEfRepository<Contracts, int>, IContractsRepository
{
    public ContractsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<Contracts?> GetWithDocumentInfoAsync(int contractId, CancellationToken ct = default)
        => await _db.Set<Contracts>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractID == contractId, ct);

    public async Task FreezeDocumentAsync(int contractId, int documentId, int templateVersion, CancellationToken ct = default)
    {
        var entity = await _db.Set<Contracts>()
            .FirstOrDefaultAsync(x => x.ContractID == contractId, ct)
            ?? throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

        entity.GeneratedDocumentId = documentId;
        entity.TemplateVersionUsed = templateVersion;
        entity.IsDocumentFrozen    = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnfreezeDocumentAsync(int contractId, CancellationToken ct = default)
    {
        var entity = await _db.Set<Contracts>()
            .FirstOrDefaultAsync(x => x.ContractID == contractId, ct)
            ?? throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

        entity.IsDocumentFrozen = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CountRootContractsByCertificationAsync(int certificationId, CancellationToken ct = default)
        => await _db.Set<Contracts>()
            .AsNoTracking()
            .CountAsync(x => x.CertificationID == certificationId && x.ParentID == null, ct);
}
