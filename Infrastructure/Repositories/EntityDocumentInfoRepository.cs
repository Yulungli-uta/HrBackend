using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>Implementación EF de <see cref="IEntityDocumentInfoRepository"/> (solo lectura, sin seguimiento de cambios).</summary>
public sealed class EntityDocumentInfoRepository : IEntityDocumentInfoRepository
{
    private readonly AppDbContext _db;

    public EntityDocumentInfoRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<EntityDocumentInfo?> GetContractInfoAsync(int contractId, CancellationToken ct = default)
    {
        var row = await _db.Set<Contracts>().AsNoTracking()
            .Where(c => c.ContractID == contractId)
            .Select(c => new { c.ContractCode, c.StartDate })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : new EntityDocumentInfo(row.ContractCode, DateOnly.FromDateTime(row.StartDate));
    }

    public async Task<EntityDocumentInfo?> GetPersonnelActionInfoAsync(int actionId, CancellationToken ct = default)
    {
        var row = await _db.Set<PersonnelAction>().AsNoTracking()
            .Where(a => a.ActionId == actionId)
            .Select(a => new { a.ActionNumber, a.ActionDate })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : new EntityDocumentInfo(row.ActionNumber, row.ActionDate);
    }

    public Task<string?> GetDocumentTypeNameAsync(int documentTypeId, CancellationToken ct = default) =>
        _db.RefTypes.AsNoTracking()
            .Where(t => t.TypeId == documentTypeId)
            .Select(t => (string?)t.Name)
            .FirstOrDefaultAsync(ct);
}
