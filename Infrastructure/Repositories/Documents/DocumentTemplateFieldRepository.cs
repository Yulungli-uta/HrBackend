using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories.Documents;

/// <summary>
/// Implementación de <see cref="IDocumentTemplateFieldRepository"/> usando EF Core + LINQ.
/// </summary>
public sealed class DocumentTemplateFieldRepository : IDocumentTemplateFieldRepository
{
    private readonly AppDbContext _db;

    public DocumentTemplateFieldRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentTemplateField>> GetByTemplateIdAsync(int templateId, CancellationToken ct = default)
        => await _db.DocumentTemplateFields
            .AsNoTracking()
            .Where(f => f.TemplateId == templateId)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<DocumentTemplateField?> GetByIdAsync(int fieldId, CancellationToken ct = default)
        => await _db.DocumentTemplateFields
            .FirstOrDefaultAsync(f => f.FieldId == fieldId, ct);

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(int templateId, string fieldName, int? excludeId = null, CancellationToken ct = default)
        => await _db.DocumentTemplateFields
            .AsNoTracking()
            .AnyAsync(f => f.TemplateId == templateId
                        && f.FieldName == fieldName
                        && (!excludeId.HasValue || f.FieldId != excludeId.Value), ct);

    /// <inheritdoc/>
    public async Task<int> CreateAsync(DocumentTemplateField field, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(field);
        _db.DocumentTemplateFields.Add(field);
        await _db.SaveChangesAsync(ct);
        return field.FieldId;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(DocumentTemplateField field, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(field);
        _db.DocumentTemplateFields.Update(field);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int fieldId, CancellationToken ct = default)
    {
        await _db.DocumentTemplateFields
            .Where(f => f.FieldId == fieldId)
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc/>
    public async Task ReplaceFieldsAsync(int templateId, IEnumerable<DocumentTemplateField> fields, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fields);

        // Eliminar todos los campos existentes de la plantilla
        await _db.DocumentTemplateFields
            .Where(f => f.TemplateId == templateId)
            .ExecuteDeleteAsync(ct);

        // Insertar los nuevos campos
        var fieldList = fields.ToList();
        fieldList.ForEach(f => f.TemplateId = templateId);
        _db.DocumentTemplateFields.AddRange(fieldList);
        await _db.SaveChangesAsync(ct);
    }
}
