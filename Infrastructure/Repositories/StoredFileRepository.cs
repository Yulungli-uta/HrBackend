using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class StoredFileRepository : ServiceAwareEfRepository<StoredFile, int>, IStoredFileRepository
    {
        private readonly DbContext _db;

        private readonly IAppClock _clock;

        public StoredFileRepository(WsUtaSystem.Data.AppDbContext db, IAppClock clock) : base(db)
        {
            _clock = clock;
            _db = db;
        }

        public async Task<IEnumerable<StoredFile>> GetByEntityAsync(
            string directoryCode,
            string entityType,
            string entityId,
            int? uploadYear,
            int? status,
            CancellationToken ct)
        {
            var q = _db.Set<StoredFile>().AsNoTracking().AsQueryable();

            q = q.Where(x =>
                x.DirectoryCode == directoryCode &&
                x.EntityType == entityType &&
                x.EntityId == entityId);

            if (uploadYear.HasValue)
                q = q.Where(x => x.UploadYear == uploadYear.Value);

            if (status.HasValue)
                q = q.Where(x => x.Status == status.Value);

            return await q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        }

        public async Task<StoredFile?> GetByGuidAsync(Guid fileGuid, CancellationToken ct)
        {
            return await _db.Set<StoredFile>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FileGuid == fileGuid, ct);
        }

        public async Task SoftDeleteAsync(int id, int? deletedBy, CancellationToken ct)
        {
            var entity = await _db.Set<StoredFile>().FirstOrDefaultAsync(x => x.FileId == id, ct);
            if (entity is null) return;

            entity.Status = 2; // Deleted
            entity.DeletedAt = _clock.Now; // o _clock.Now si tu sistema usa local time
            entity.DeletedBy = deletedBy;

            await _db.SaveChangesAsync(ct);
        }
    }
}