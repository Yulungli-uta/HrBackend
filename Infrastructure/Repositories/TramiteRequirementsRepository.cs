using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class TramiteRequirementsRepository : ServiceAwareEfRepository<TramiteRequirement, int>, ITramiteRequirementsRepository
{
    private readonly AppDbContext _db;

    public TramiteRequirementsRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<List<TramiteRequirement>> GetByModuleAsync(int moduleTypeId, CancellationToken ct)
    {
        return await _db.TramiteRequirements
            .AsNoTracking()
            .Include(r => r.ModuleType)
            .Include(r => r.DocumentType)
            .Where(r => r.ModuleTypeId == moduleTypeId && r.IsActive)
            .OrderBy(r => r.SpecificTypeId)
            .ThenBy(r => r.RequirementId)
            .ToListAsync(ct);
    }

    public async Task<List<int>> GetRequiredDocumentTypeIdsAsync(int moduleTypeId, int? specificTypeId, CancellationToken ct)
    {
        return await _db.TramiteRequirements
            .AsNoTracking()
            .Where(r => r.ModuleTypeId == moduleTypeId && r.IsActive && r.IsRequired
                     && (r.SpecificTypeId == null || r.SpecificTypeId == specificTypeId))
            .Select(r => r.DocumentTypeId)
            .Distinct()
            .ToListAsync(ct);
    }
}
