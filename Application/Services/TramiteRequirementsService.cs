using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.TramiteRequirements;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <inheritdoc cref="ITramiteRequirementsService"/>
public class TramiteRequirementsService : ITramiteRequirementsService
{
    private readonly ITramiteRequirementsRepository _repository;
    private readonly AppDbContext _db;

    public TramiteRequirementsService(
        ITramiteRequirementsRepository repository,
        AppDbContext db)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<List<AccessibleModuleDto>> GetAccessibleModulesAsync(CancellationToken ct)
    {
        return await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == "ACCESS_MODULE_TYPE")
            .OrderBy(r => r.Name)
            .Select(r => new AccessibleModuleDto
            {
                ModuleTypeId = r.TypeId,
                ModuleTypeName = r.Name,
                ModuleTypeDescription = r.Description,
            })
            .ToListAsync(ct);
    }

    public async Task<List<TramiteRequirementDto>> GetByModuleAsync(int moduleTypeId, CancellationToken ct)
    {
        var items = await _repository.GetByModuleAsync(moduleTypeId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<List<TramiteRequirementDto>> GetApplicableAsync(int moduleTypeId, int? specificTypeId, CancellationToken ct)
    {
        var items = await _db.TramiteRequirements
            .AsNoTracking()
            .Include(r => r.ModuleType)
            .Include(r => r.DocumentType)
            .Where(r => r.ModuleTypeId == moduleTypeId && r.IsActive
                     && (r.SpecificTypeId == null || r.SpecificTypeId == specificTypeId))
            .OrderBy(r => r.SpecificTypeId)
            .ThenBy(r => r.DocumentType!.Name)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    public async Task<TramiteRequirementDto> CreateAsync(TramiteRequirementCreateDto dto, int? createdBy, CancellationToken ct)
    {
        var duplicate = await _db.TramiteRequirements
            .AsNoTracking()
            .AnyAsync(r => r.ModuleTypeId == dto.ModuleTypeId
                        && r.SpecificTypeId == dto.SpecificTypeId
                        && r.DocumentTypeId == dto.DocumentTypeId, ct);
        if (duplicate)
            throw new InvalidOperationException(
                "Ya existe un requisito configurado para ese documento en el módulo y tipo específico seleccionados.");

        var entity = new TramiteRequirement
        {
            ModuleTypeId = dto.ModuleTypeId,
            SpecificTypeId = dto.SpecificTypeId,
            DocumentTypeId = dto.DocumentTypeId,
            IsRequired = dto.IsRequired,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = createdBy,
        };

        try
        {
            await _repository.AddAsync(entity, ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException(
                "Ya existe un requisito configurado para ese documento en el módulo y tipo específico seleccionados.");
        }

        var withNames = await _db.TramiteRequirements
            .AsNoTracking()
            .Include(r => r.ModuleType)
            .Include(r => r.DocumentType)
            .FirstAsync(r => r.RequirementId == entity.RequirementId, ct);

        return ToDto(withNames);
    }

    public async Task UpdateAsync(int requirementId, TramiteRequirementUpdateDto dto, int? updatedBy, CancellationToken ct)
    {
        var current = await _db.TramiteRequirements.FirstOrDefaultAsync(r => r.RequirementId == requirementId, ct)
            ?? throw new KeyNotFoundException($"Requisito {requirementId} no encontrado.");

        current.IsRequired = dto.IsRequired;
        current.IsActive = dto.IsActive;
        current.UpdatedAt = DateTime.Now;
        current.UpdatedBy = updatedBy;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int requirementId, CancellationToken ct)
    {
        var current = await _db.TramiteRequirements.FirstOrDefaultAsync(r => r.RequirementId == requirementId, ct)
            ?? throw new KeyNotFoundException($"Requisito {requirementId} no encontrado.");

        _db.TramiteRequirements.Remove(current);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ValidateRequiredDocumentsAsync(
        int moduleTypeId, int? specificTypeId, string entityType, string entityId, CancellationToken ct)
    {
        var requiredDocTypeIds = await _repository.GetRequiredDocumentTypeIdsAsync(moduleTypeId, specificTypeId, ct);
        if (requiredDocTypeIds.Count == 0) return; // sin requisitos configurados: no bloquea (comportamiento actual)

        var uploadedDocTypeIds = await _db.StoredFiles
            .AsNoTracking()
            .Where(f => f.EntityType == entityType && f.EntityId == entityId && f.Status == 1
                     && f.DocumentTypeId != null && requiredDocTypeIds.Contains(f.DocumentTypeId.Value))
            .Select(f => f.DocumentTypeId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var missingIds = requiredDocTypeIds.Except(uploadedDocTypeIds).ToList();
        if (missingIds.Count == 0) return;

        var missingNames = await _db.RefTypes
            .AsNoTracking()
            .Where(r => missingIds.Contains(r.TypeId))
            .Select(r => r.Name)
            .ToListAsync(ct);

        throw new InvalidOperationException(
            $"Faltan documentos obligatorios: {string.Join(", ", missingNames)}.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private static TramiteRequirementDto ToDto(TramiteRequirement r) => new()
    {
        RequirementId = r.RequirementId,
        ModuleTypeId = r.ModuleTypeId,
        ModuleTypeName = r.ModuleType?.Name,
        SpecificTypeId = r.SpecificTypeId,
        DocumentTypeId = r.DocumentTypeId,
        DocumentTypeName = r.DocumentType?.Name,
        IsRequired = r.IsRequired,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
