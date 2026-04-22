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

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new DocumentTemplateSummaryDto(
                t.TemplateId,
                t.TemplateCode,
                t.Name,
                t.Description,
                t.TemplateType,
                t.Version,
                t.LayoutType,   // enum real
                t.Status,       // enum real
                t.RequiresSignature,
                t.RequiresApproval,
                _db.DocumentTemplateFields.Count(f => f.TemplateId == t.TemplateId),
                t.CreatedAt,
                t.UpdatedAt
            ))
            .ToListAsync(ct);
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
        // DocumentTemplate.Status es enum real → EF Core aplica la conversión automáticamente
        await _db.DocumentTemplates
            .Where(t => t.TemplateId == templateId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow),
            ct);
    }
}
