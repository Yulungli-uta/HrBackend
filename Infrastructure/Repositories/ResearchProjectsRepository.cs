using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.ResearchProjects;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class ResearchProjectsRepository : IResearchProjectsRepository
{
    private readonly AppDbContext _db;

    private readonly IAppClock _clock;

    public ResearchProjectsRepository(AppDbContext db, IAppClock clock)
    {
        _clock = clock;
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<(IReadOnlyList<ResearchProjectListRow> Items, long Total)> SearchAsync(
        string? search, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ResearchProjects.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Name, term) || EF.Functions.Like(p.ApprovalResolutionNumber, term));
        }

        // Rango de fechas: el período del proyecto (inicio a fin) se cruza con el rango pedido.
        if (from.HasValue) query = query.Where(p => p.EndDate >= from.Value);
        if (to.HasValue) query = query.Where(p => p.StartDate <= to.Value);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.ResearchProjectId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ResearchProjectListRow(
                p.ResearchProjectId, p.Name, p.StartDate, p.EndDate, p.HasExtension,
                p.Participations.Count(x => !x.IsDeleted)))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<ResearchProject?> GetByIdAsync(int id, bool track, CancellationToken ct = default)
    {
        var query = _db.ResearchProjects
            .Include(p => p.Participations.Where(x => !x.IsDeleted))
            .Where(p => p.ResearchProjectId == id && !p.IsDeleted);
        if (!track) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ResearchProject>> GetByPersonAsync(int personId, CancellationToken ct = default)
        => await _db.ResearchProjects
            .AsNoTracking()
            .Include(p => p.Participations.Where(x => !x.IsDeleted))
            .Where(p => !p.IsDeleted && p.Participations.Any(x => !x.IsDeleted && !x.IsExternal && x.PersonId == personId))
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);

    public async Task SaveAsync(
        ResearchProject project, bool isNew, IReadOnlyDictionary<ResearchDocumentKind, StoredFile> newFiles, CancellationToken ct = default)
    {
        // El proyecto usa una estrategia de ejecución con reintentos: las transacciones manuales deben correr dentro de ella.
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var replacedIds = new List<int>();

                foreach (var (kind, file) in newFiles)
                {
                    _db.StoredFiles.Add(file);
                }
                if (newFiles.Count > 0) await _db.SaveChangesAsync(ct);

                foreach (var (kind, file) in newFiles)
                {
                    switch (kind)
                    {
                        case ResearchDocumentKind.Approval:
                            if (!isNew && project.ApprovalFileId.HasValue) replacedIds.Add(project.ApprovalFileId.Value);
                            project.ApprovalFileId = file.FileId;
                            break;
                        case ResearchDocumentKind.Extension:
                            if (!isNew && project.ExtensionFileId.HasValue) replacedIds.Add(project.ExtensionFileId.Value);
                            project.ExtensionFileId = file.FileId;
                            break;
                        case ResearchDocumentKind.Completion:
                            if (!isNew && project.CompletionFileId.HasValue) replacedIds.Add(project.CompletionFileId.Value);
                            project.CompletionFileId = file.FileId;
                            break;
                    }
                }

                if (isNew) _db.ResearchProjects.Add(project);
                await _db.SaveChangesAsync(ct);

                // Los archivos se insertaron antes del proyecto (el proyecto los referencia por FK): ahora que
                // existe el Id del proyecto, se completa el EntityId que los vincula a él.
                if (isNew && newFiles.Count > 0)
                {
                    foreach (var file in newFiles.Values) file.EntityId = project.ResearchProjectId.ToString();
                    await _db.SaveChangesAsync(ct);
                }

                if (replacedIds.Count > 0)
                {
                    var old = await _db.StoredFiles.Where(f => replacedIds.Contains(f.FileId)).ToListAsync(ct);
                    foreach (var f in old)
                    {
                        f.Status = 2; // eliminado lógico; el archivo físico se conserva
                        f.DeletedAt = _clock.Now;
                    }
                    await _db.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var project = await _db.ResearchProjects
            .Include(p => p.Participations)
            .FirstOrDefaultAsync(p => p.ResearchProjectId == id && !p.IsDeleted, ct);
        if (project is null) return;

        project.IsDeleted = true;
        foreach (var part in project.Participations) part.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, StoredFile>> GetStoredFilesAsync(IEnumerable<int> fileIds, CancellationToken ct = default)
    {
        var ids = fileIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, StoredFile>();
        return await _db.StoredFiles.AsNoTracking().Where(f => ids.Contains(f.FileId)).ToDictionaryAsync(f => f.FileId, ct);
    }

    public Task<StoredFile?> GetStoredFileByGuidAsync(Guid fileGuid, CancellationToken ct = default)
        => _db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(f => f.FileGuid == fileGuid && f.Status == 1, ct);

    public async Task<bool> IsParticipantOfFileAsync(Guid fileGuid, int personId, CancellationToken ct = default)
    {
        var fileId = await _db.StoredFiles.AsNoTracking()
            .Where(f => f.FileGuid == fileGuid && f.Status == 1)
            .Select(f => (int?)f.FileId)
            .FirstOrDefaultAsync(ct);
        if (fileId is null) return false;

        return await _db.ResearchProjects.AsNoTracking().AnyAsync(p =>
            !p.IsDeleted
            && (p.ApprovalFileId == fileId || p.ExtensionFileId == fileId || p.CompletionFileId == fileId)
            && p.Participations.Any(x => !x.IsDeleted && !x.IsExternal && x.PersonId == personId), ct);
    }

    public async Task<IReadOnlyList<ResearchRefTypeLite>> GetRefTypesAsync(IEnumerable<string> categories, CancellationToken ct = default)
    {
        var cats = categories.ToList();
        return await _db.RefTypes.AsNoTracking()
            .Where(r => cats.Contains(r.Category))
            .Select(r => new ResearchRefTypeLite(r.TypeId, r.Category, r.Name, r.Description))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, ResearchPersonInfo>> GetPeopleAsync(IEnumerable<int> personIds, CancellationToken ct = default)
    {
        var ids = personIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, ResearchPersonInfo>();
        return await _db.People.AsNoTracking()
            .Where(p => ids.Contains(p.PersonId))
            .Select(p => new ResearchPersonInfo(p.PersonId, p.IdCard, p.LastName + " " + p.FirstName))
            .ToDictionaryAsync(p => p.PersonId, ct);
    }

    public async Task<IReadOnlyList<ResearchPersonLookupDto>> SearchPeopleAsync(string term, int take, CancellationToken ct = default)
    {
        var like = $"%{term.Trim()}%";
        var startsWith = $"{term.Trim()}%";
        return await _db.People.AsNoTracking()
            .Where(p => EF.Functions.Like(p.IdCard, startsWith)
                        || EF.Functions.Like(p.LastName + " " + p.FirstName, like)
                        || EF.Functions.Like(p.FirstName + " " + p.LastName, like))
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Take(take)
            .Select(p => new ResearchPersonLookupDto(p.PersonId, p.IdCard, p.LastName + " " + p.FirstName))
            .ToListAsync(ct);
    }
}
