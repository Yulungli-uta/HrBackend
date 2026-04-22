using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories.Documents;

/// <summary>
/// Implementación de <see cref="IGeneratedDocumentRepository"/> usando EF Core + LINQ.
/// Proyección directa a DTOs con joins a Employees, People y DocumentTemplates.
/// <para>
/// <see cref="GeneratedDocument.EntityType"/> es un enum real con HasConversion en EF Core.
/// No se requiere <c>Enum.Parse</c> en proyecciones LINQ; EF Core aplica la conversión automáticamente.
/// </para>
/// </summary>
public sealed class GeneratedDocumentRepository : IGeneratedDocumentRepository
{
    private readonly AppDbContext _db;

    public GeneratedDocumentRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<PagedDocumentResult> GetPagedAsync(DocumentQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = from doc in _db.GeneratedDocuments.AsNoTracking()
                    join tmpl in _db.DocumentTemplates.AsNoTracking()
                        on doc.TemplateId equals tmpl.TemplateId
                    join emp in _db.Employees.AsNoTracking()
                        on doc.EmployeeId equals emp.EmployeeId
                    join person in _db.People.AsNoTracking()
                        on emp.PersonID equals person.PersonId
                    select new { doc, tmpl, emp, person };

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.doc.EmployeeId == filter.EmployeeId.Value);

        if (filter.TemplateId.HasValue)
            query = query.Where(x => x.doc.TemplateId == filter.TemplateId.Value);

        // GeneratedDocument.EntityType es enum real → comparar directo
        if (filter.EntityType.HasValue)
            query = query.Where(x => x.doc.EntityType == filter.EntityType.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.doc.Status == filter.Status);

        if (filter.StartDate.HasValue)
            query = query.Where(x => x.doc.CreatedAt.HasValue
                && DateOnly.FromDateTime(x.doc.CreatedAt.Value) >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(x => x.doc.CreatedAt.HasValue
                && DateOnly.FromDateTime(x.doc.CreatedAt.Value) <= filter.EndDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.doc.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new GeneratedDocumentSummaryDto(
                x.doc.DocumentId,
                x.doc.TemplateId,
                x.tmpl.Name,
                x.tmpl.TemplateCode,
                x.doc.EmployeeId,
                x.person.FirstName + " " + x.person.LastName,
                x.person.IdCard,
                x.doc.EntityType,   // enum real
                x.doc.EntityId,
                x.doc.DocumentNumber,
                x.doc.FileName,
                x.doc.Status,
                x.doc.IsSigned,
                x.doc.IsApproved,
                x.doc.CreatedAt,
                x.doc.CreatedBy
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

        return new PagedDocumentResult(items, totalCount, filter.Page, filter.PageSize, totalPages);
    }

    /// <inheritdoc/>
    public async Task<GeneratedDocumentDetailDto?> GetDetailByIdAsync(int documentId, CancellationToken ct = default)
    {
        var result = await (
            from doc in _db.GeneratedDocuments.AsNoTracking()
            join tmpl in _db.DocumentTemplates.AsNoTracking()
                on doc.TemplateId equals tmpl.TemplateId
            join emp in _db.Employees.AsNoTracking()
                on doc.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking()
                on emp.PersonID equals person.PersonId
            where doc.DocumentId == documentId
            select new { doc, tmpl, emp, person }
        ).FirstOrDefaultAsync(ct);

        if (result is null) return null;

        var fields = await _db.GeneratedDocumentFields
            .AsNoTracking()
            .Where(f => f.DocumentId == documentId)
            .OrderBy(f => f.FieldName)
            .Select(f => new GeneratedDocumentFieldDto(
                f.DocumentFieldId,
                f.FieldName,
                f.FieldValue,
                f.SourceType,
                f.WasOverridden
            ))
            .ToListAsync(ct);

        return new GeneratedDocumentDetailDto(
            result.doc.DocumentId,
            result.doc.TemplateId,
            result.tmpl.Name,
            result.tmpl.TemplateCode,
            result.doc.EmployeeId,
            result.person.FirstName + " " + result.person.LastName,
            result.person.IdCard,
            result.doc.EntityType,  // enum real
            result.doc.EntityId,
            result.doc.DocumentNumber,
            result.doc.FileName,
            result.doc.StoredFileId,
            result.doc.Status,
            result.doc.Notes,
            result.doc.IsSigned,
            result.doc.SignedAt,
            result.doc.SignedBy,
            result.doc.IsApproved,
            result.doc.ApprovedAt,
            result.doc.ApprovedBy,
            fields,
            result.doc.CreatedAt,
            result.doc.CreatedBy,
            result.doc.UpdatedAt,
            result.doc.UpdatedBy
        );
    }

    /// <inheritdoc/>
    public async Task<GeneratedDocument?> GetByIdAsync(int documentId, CancellationToken ct = default)
        => await _db.GeneratedDocuments
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GeneratedDocumentSummaryDto>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await (
            from doc in _db.GeneratedDocuments.AsNoTracking()
            join tmpl in _db.DocumentTemplates.AsNoTracking()
                on doc.TemplateId equals tmpl.TemplateId
            join emp in _db.Employees.AsNoTracking()
                on doc.EmployeeId equals emp.EmployeeId
            join person in _db.People.AsNoTracking()
                on emp.PersonID equals person.PersonId
            where doc.EmployeeId == employeeId
            orderby doc.CreatedAt descending
            select new GeneratedDocumentSummaryDto(
                doc.DocumentId,
                doc.TemplateId,
                tmpl.Name,
                tmpl.TemplateCode,
                doc.EmployeeId,
                person.FirstName + " " + person.LastName,
                person.IdCard,
                doc.EntityType, // enum real
                doc.EntityId,
                doc.DocumentNumber,
                doc.FileName,
                doc.Status,
                doc.IsSigned,
                doc.IsApproved,
                doc.CreatedAt,
                doc.CreatedBy
            )
        ).ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> CreateAsync(GeneratedDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        _db.GeneratedDocuments.Add(document);
        await _db.SaveChangesAsync(ct);
        return document.DocumentId;
    }

    /// <inheritdoc/>
    public async Task UpdateStatusAsync(int documentId, string status, string? notes, CancellationToken ct = default)
    {
        await _db.GeneratedDocuments
            .Where(d => d.DocumentId == documentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, status)
                .SetProperty(d => d.Notes, notes)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc/>
    public async Task SignAsync(int documentId, int signedBy, CancellationToken ct = default)
    {
        await _db.GeneratedDocuments
            .Where(d => d.DocumentId == documentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.IsSigned, true)
                .SetProperty(d => d.SignedAt, DateTime.UtcNow)
                .SetProperty(d => d.SignedBy, signedBy)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc/>
    public async Task ApproveAsync(int documentId, int approvedBy, string? notes, CancellationToken ct = default)
    {
        await _db.GeneratedDocuments
            .Where(d => d.DocumentId == documentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.IsApproved, true)
                .SetProperty(d => d.ApprovedAt, DateTime.UtcNow)
                .SetProperty(d => d.ApprovedBy, approvedBy)
                .SetProperty(d => d.Status, "APPROVED")
                .SetProperty(d => d.Notes, notes)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc/>
    public async Task UpdateStoredFileAsync(int documentId, int storedFileId, string fileName, CancellationToken ct = default)
    {
        await _db.GeneratedDocuments
            .Where(d => d.DocumentId == documentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.StoredFileId, storedFileId)
                .SetProperty(d => d.FileName, fileName)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
            ct);
    }
}
