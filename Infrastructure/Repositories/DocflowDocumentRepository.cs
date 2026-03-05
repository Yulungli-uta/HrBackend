using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class DocflowDocumentRepository : IDocflowDocumentRepository
    {
        private readonly AppDbContext _db;
        public DocflowDocumentRepository(AppDbContext db) => _db = db;

        public Task<DocflowDocument?> GetByIdAsync(Guid documentId, CancellationToken ct) =>
            _db.DocflowDocuments.FirstOrDefaultAsync(x => x.DocumentId == documentId, ct);

        public async Task<List<DocflowDocument>> GetVisibleByInstanceAsync(Guid instanceId, int currentUserDepartmentId, CancellationToken ct)
        {
            // Visibility:
            // 1 = visible a todos con acceso al expediente
            // 2 = solo depto creador
            return await _db.DocflowDocuments.AsNoTracking()
                .Where(d => d.InstanceId == instanceId && !d.IsDeleted)
                .Where(d =>
                    d.Visibility == DocflowVisibility.PublicWithinCase ||
                    (d.Visibility == DocflowVisibility.PrivateToUploaderDept && d.CreatedByDepartmentId == currentUserDepartmentId)
                )
                .OrderBy(d => d.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task CreateAsync(DocflowDocument doc, CancellationToken ct)
        {
            _db.DocflowDocuments.Add(doc);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(DocflowDocument doc, CancellationToken ct)
        {
            _db.DocflowDocuments.Update(doc);
            await _db.SaveChangesAsync(ct);
        }

        public Task<List<DocflowFileVersion>> GetVersionsAsync(Guid documentId, CancellationToken ct) =>
            _db.DocflowFileVersions.AsNoTracking()
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(ct);

        public Task<DocflowFileVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken ct) =>
            _db.DocflowFileVersions.AsNoTracking().FirstOrDefaultAsync(x => x.VersionId == versionId, ct);

        public async Task CreateVersionAsync(DocflowFileVersion version, CancellationToken ct)
        {
            _db.DocflowFileVersions.Add(version);
            await _db.SaveChangesAsync(ct);
        }
    }
}

