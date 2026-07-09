using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories.Documents;

/// <summary>
/// Implementación de <see cref="IDocumentTemplateRepository"/> usando EF Core + LINQ.
/// Todas las consultas de lectura usan <c>AsNoTracking()</c> para máximo rendimiento.
/// Los modelos <see cref="DocumentTemplate"/> y <see cref="DocumentTemplateField"/> usan
/// enums reales con conversión EF Core (HasConversion), por lo que no se requiere
/// <c>Enum.Parse</c> en proyecciones LINQ.
/// </summary>
public sealed class DocumentTemplateRepository : IDocumentTemplateRepository
{
    private readonly AppDbContext _db;

    public DocumentTemplateRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentTemplateSummaryDto>> GetAllAsync(
        string? templateType = null,
        DocumentTemplateStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _db.DocumentTemplates
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(templateType))
            query = query.Where(t => t.TemplateType == templateType);

        // DocumentTemplate.Status es enum real (HasConversion en EF) → comparar directo
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var templates = await query
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.TemplateId,
                t.TemplateCode,
                t.Name,
                t.Description,
                t.TemplateType,
                t.Version,
                t.LayoutType,
                t.Status,
                t.RequiresSignature,
                t.RequiresApproval,
                FieldCount = _db.DocumentTemplateFields.Count(f => f.TemplateId == t.TemplateId),
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync(ct);

        if (templates.Count == 0) return [];

        var templateIds = templates.Select(t => t.TemplateId).ToList();

        // Tipos de contrato activos (Status != Inactivo) que usan cada plantilla como
        // predeterminada o de delegación — determina la "vigencia real" de la plantilla.
        var activeContractTypes = await _db.ContractType
            .AsNoTracking()
            .Where(c => c.Status != "I"
                     && (c.DefaultTemplateId.HasValue && templateIds.Contains(c.DefaultTemplateId.Value)
                      || c.DelegationTemplateId.HasValue && templateIds.Contains(c.DelegationTemplateId.Value)))
            .Select(c => new { c.Name, c.DefaultTemplateId, c.DelegationTemplateId })
            .ToListAsync(ct);

        // Tipos de acción de personal activos que usan cada plantilla como predeterminada.
        var activeActionTypes = await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .Where(p => p.IsActive && p.DefaultTemplateId.HasValue && templateIds.Contains(p.DefaultTemplateId.Value))
            .Select(p => new { p.Name, p.DefaultTemplateId })
            .ToListAsync(ct);

        var usageByTemplate = new Dictionary<int, List<string>>();
        void AddUsage(int? templateId, string name)
        {
            if (!templateId.HasValue || !templateIds.Contains(templateId.Value)) return;
            if (!usageByTemplate.TryGetValue(templateId.Value, out var names))
                usageByTemplate[templateId.Value] = names = [];
            if (!names.Contains(name)) names.Add(name);
        }
        foreach (var c in activeContractTypes)
        {
            AddUsage(c.DefaultTemplateId, c.Name);
            AddUsage(c.DelegationTemplateId, c.Name);
        }
        foreach (var p in activeActionTypes)
            AddUsage(p.DefaultTemplateId, p.Name);

        return templates.Select(t =>
        {
            var usedBy = usageByTemplate.TryGetValue(t.TemplateId, out var names) ? names : [];
            return new DocumentTemplateSummaryDto(
                t.TemplateId,
                t.TemplateCode,
                t.Name,
                t.Description,
                t.TemplateType,
                t.Version,
                t.LayoutType,
                t.Status,
                t.RequiresSignature,
                t.RequiresApproval,
                t.FieldCount,
                t.CreatedAt,
                t.UpdatedAt,
                IsInUse: usedBy.Count > 0,
                UsedBy: usedBy
            );
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplateDetailDto?> GetDetailByIdAsync(int templateId, CancellationToken ct = default)
    {
        var template = await _db.DocumentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateId == templateId, ct);

        if (template is null) return null;

        var fields = await _db.DocumentTemplateFields
            .AsNoTracking()
            .Where(f => f.TemplateId == templateId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new DocumentTemplateFieldDto(
                f.FieldId,
                f.TemplateId,
                f.FieldName,
                f.Label,
                f.SourceType,   // enum real (HasConversion en EF)
                f.SourceProperty,
                f.DataType,
                f.FormatPattern,
                f.DefaultValue,
                f.IsRequired,
                f.IsEditable,
                f.SortOrder,
                f.HelpText
            ))
            .ToListAsync(ct);

        return new DocumentTemplateDetailDto(
            template.TemplateId,
            template.TemplateCode,
            template.Name,
            template.Description,
            template.TemplateType,
            template.Version,
            template.LayoutType,    // enum real
            template.Status,        // enum real
            template.HtmlContent,
            template.CssStyles,
            template.MetaJson,
            template.RequiresSignature,
            template.RequiresApproval,
            fields,
            template.CreatedAt,
            template.CreatedBy,
            template.UpdatedAt,
            template.UpdatedBy
        );
    }

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetByCodeAsync(string templateCode, CancellationToken ct = default)
        => await _db.DocumentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateCode == templateCode, ct);

    /// <inheritdoc/>
    public async Task<DocumentTemplate?> GetByIdAsync(int templateId, CancellationToken ct = default)
        => await _db.DocumentTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByCodeAsync(string templateCode, int? excludeId = null, CancellationToken ct = default)
        => await _db.DocumentTemplates
            .AsNoTracking()
            .AnyAsync(t => t.TemplateCode == templateCode
                        && (!excludeId.HasValue || t.TemplateId != excludeId.Value), ct);

    /// <inheritdoc/>
    public async Task<int> CreateAsync(DocumentTemplate template, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        _db.DocumentTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return template.TemplateId;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(DocumentTemplate template, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        _db.DocumentTemplates.Update(template);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdateStatusAsync(int templateId, DocumentTemplateStatus status, CancellationToken ct = default)
    {
        await _db.DocumentTemplates
            .Where(t => t.TemplateId == templateId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TemplateVersionSummaryDto>> GetVersionsByCodeAsync(string templateCode, CancellationToken ct = default)
        => await _db.DocumentTemplates
            .AsNoTracking()
            .Where(t => t.TemplateCode == templateCode)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TemplateVersionSummaryDto(
                t.TemplateId,
                t.TemplateCode,
                t.Version,
                t.Status,
                t.Name,
                t.CreatedAt,
                t.CreatedBy,
                t.UpdatedAt,
                t.UpdatedBy))
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task ArchiveOtherPublishedVersionsAsync(string templateCode, int keepPublishedId, CancellationToken ct = default)
        => await _db.DocumentTemplates
            .Where(t => t.TemplateCode == templateCode
                     && t.TemplateId != keepPublishedId
                     && t.Status == DocumentTemplateStatus.Published)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, DocumentTemplateStatus.Archived)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
            ct);

    /// <inheritdoc/>
    public async Task RepointTemplateConsumersAsync(int oldTemplateId, int newTemplateId, CancellationToken ct = default)
    {
        await _db.ContractType
            .Where(ct2 => ct2.DefaultTemplateId == oldTemplateId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ct2 => ct2.DefaultTemplateId, newTemplateId),
            ct);

        await _db.ContractType
            .Where(ct2 => ct2.DelegationTemplateId == oldTemplateId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ct2 => ct2.DelegationTemplateId, newTemplateId),
            ct);

        await _db.Set<PersonnelActionType>()
            .Where(pat => pat.DefaultTemplateId == oldTemplateId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(pat => pat.DefaultTemplateId, newTemplateId),
            ct);
    }

    /// <inheritdoc/>
    public async Task<(int ContractTypeId, string ContractTypeName, string? ContractText)?> GetContractTypeTextAsync(int contractTypeId, CancellationToken ct = default)
    {
        var row = await _db.ContractType
            .AsNoTracking()
            .Where(ct2 => ct2.ContractTypeId == contractTypeId)
            .Select(ct2 => new { ct2.ContractTypeId, ct2.Name, ct2.ContractText })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;
        return (row.ContractTypeId, row.Name, row.ContractText);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TemplateContractTypeOptionDto>> GetContractTypesForTemplateAsync(int templateId, CancellationToken ct = default)
    {
        var template = await _db.DocumentTemplates
            .AsNoTracking()
            .Where(t => t.TemplateId == templateId)
            .Select(t => new { t.TemplateType })
            .FirstOrDefaultAsync(ct);

        if (template is null) return [];

        var familyTypeId = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == "DOCUMENT_TEMPLATE_TYPE" && r.Name == template.TemplateType)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);

        var linked = await _db.ContractType
            .AsNoTracking()
            .Where(c => c.DefaultTemplateId == templateId
                     || (familyTypeId.HasValue && c.DocumentTemplateTypeId == familyTypeId.Value))
            .OrderByDescending(c => c.DefaultTemplateId == templateId)
            .ThenBy(c => c.Name)
            .Select(c => new TemplateContractTypeOptionDto(
                c.ContractTypeId,
                c.Name,
                c.DefaultTemplateId == templateId
            ))
            .ToListAsync(ct);

        if (linked.Count > 0) return linked;

        // Fallback: mientras ningún ContractType tenga la relación con plantillas poblada
        // (DocumentTemplateTypeId/DefaultTemplateId), se muestran todos los tipos activos
        // para no dejar el selector vacío.
        return await _db.ContractType
            .AsNoTracking()
            .Where(c => c.Status == "1")
            .OrderBy(c => c.Name)
            .Select(c => new TemplateContractTypeOptionDto(c.ContractTypeId, c.Name, false))
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TemplateActionTypeOptionDto>> GetActionTypesForTemplateAsync(int templateId, CancellationToken ct = default)
    {
        var template = await _db.DocumentTemplates
            .AsNoTracking()
            .Where(t => t.TemplateId == templateId)
            .Select(t => new { t.TemplateType })
            .FirstOrDefaultAsync(ct);

        if (template is null) return [];

        return await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.DefaultTemplateId == templateId)
            .ThenBy(p => p.Name)
            .Select(p => new TemplateActionTypeOptionDto(
                p.PersonnelActionTypeId,
                p.Name,
                p.DefaultTemplateId == templateId
            ))
            .ToListAsync(ct);
    }
}
