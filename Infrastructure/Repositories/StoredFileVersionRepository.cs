using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.StoredFileVersions;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <inheritdoc cref="IStoredFileVersionRepository"/>
public sealed class StoredFileVersionRepository : IStoredFileVersionRepository
{
    /// <summary>Estado de <c>StoredFile</c> para archivos reemplazados por una versión nueva (1 activo, 2 eliminado, 3 archivado).</summary>
    private const byte StoredFileArchived = 3;

    private readonly AppDbContext _db;
    private readonly IAppClock _clock;

    public StoredFileVersionRepository(AppDbContext db, IAppClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<StoredFileVersionDto>> GetVersionsAsync(string entityType, string entityId, string slotCode, CancellationToken ct)
    {
        var rows = await (
            from v in _db.Set<StoredFileVersion>().AsNoTracking()
            join f in _db.StoredFiles.AsNoTracking() on v.StoredFileId equals f.FileId
            where v.EntityType == entityType && v.EntityId == entityId && v.SlotCode == slotCode
            orderby v.VersionNo descending
            select new { v, f.FileGuid, f.OriginalFileName, f.StoredFileName, f.SizeBytes }
        ).ToListAsync(ct);

        var employeeIds = rows
            .SelectMany(r => new[] { r.v.UploadedBy, r.v.ReplacedBy })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var names = employeeIds.Count == 0
            ? new Dictionary<int, string>()
            : await (
                from e in _db.Employees.AsNoTracking()
                join p in _db.People.AsNoTracking() on e.PersonID equals p.PersonId
                where employeeIds.Contains(e.EmployeeId)
                select new { e.EmployeeId, Name = p.LastName + " " + p.FirstName }
            ).ToDictionaryAsync(x => x.EmployeeId, x => x.Name.Trim(), ct);

        string? NameOf(int? id) => id.HasValue && names.TryGetValue(id.Value, out var n) ? n : null;

        return rows.Select(r => new StoredFileVersionDto(
            r.v.VersionId, r.v.VersionNo, r.v.StoredFileId, r.FileGuid,
            r.OriginalFileName ?? r.StoredFileName, r.SizeBytes, r.v.IsCurrent,
            r.v.UploadedBy, NameOf(r.v.UploadedBy), r.v.UploadedAt,
            r.v.ReplacedBy, NameOf(r.v.ReplacedBy), r.v.ReplacedAt, r.v.Reason)).ToList();
    }

    public async Task<StoredFile?> GetStoredFileAsync(int storedFileId, CancellationToken ct)
        => await _db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(f => f.FileId == storedFileId, ct);

    public async Task<bool> IsCurrentVersionFileAsync(int storedFileId, CancellationToken ct)
        => await _db.Set<StoredFileVersion>().AsNoTracking().AnyAsync(v => v.StoredFileId == storedFileId && v.IsCurrent, ct);

    public async Task<int> AppendVersionAsync(
        string entityType,
        string entityId,
        string slotCode,
        int storedFileId,
        int? userId,
        string? reason,
        Func<CancellationToken, Task>? sameTransactionWork,
        CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var now = _clock.Now;
            var versions = _db.Set<StoredFileVersion>();

            var current = await versions.FirstOrDefaultAsync(
                v => v.EntityType == entityType && v.EntityId == entityId && v.SlotCode == slotCode && v.IsCurrent, ct);

            var maxNo = await versions
                .Where(v => v.EntityType == entityType && v.EntityId == entityId && v.SlotCode == slotCode)
                .Select(v => (int?)v.VersionNo)
                .MaxAsync(ct) ?? 0;

            if (current is not null)
            {
                if (current.StoredFileId == storedFileId)
                    throw new InvalidOperationException("El archivo indicado ya es la versión vigente.");

                // Primero se libera la ranura vigente: el índice único filtrado no admite dos vigentes a la vez.
                current.IsCurrent = false;
                current.ReplacedBy = userId;
                current.ReplacedAt = now;

                var old = await _db.StoredFiles.FirstOrDefaultAsync(f => f.FileId == current.StoredFileId, ct);
                if (old is not null)
                {
                    old.Status = StoredFileArchived;
                    old.UpdatedAt = now;
                    old.UpdatedBy = userId;
                }
                await _db.SaveChangesAsync(ct);
            }

            var created = new StoredFileVersion
            {
                EntityType = entityType,
                EntityId = entityId,
                SlotCode = slotCode,
                VersionNo = maxNo + 1,
                StoredFileId = storedFileId,
                IsCurrent = true,
                UploadedBy = userId,
                UploadedAt = now,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
            };
            versions.Add(created);
            await _db.SaveChangesAsync(ct);

            if (sameTransactionWork is not null)
                await sameTransactionWork(ct);

            await tx.CommitAsync(ct);
            return created.VersionNo;
        });
    }
}
