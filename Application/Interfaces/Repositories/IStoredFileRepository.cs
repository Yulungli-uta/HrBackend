using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IStoredFileRepository : IRepository<StoredFile, int>
    {
        Task<IEnumerable<StoredFile>> GetByEntityAsync(
            string directoryCode,
            string entityType,
            string entityId,
            int? uploadYear,
            int? status,
            CancellationToken ct);

        Task<StoredFile?> GetByGuidAsync(Guid fileGuid, CancellationToken ct);

        /// <summary>
        /// Soft delete (Status=2) con auditoría.
        /// </summary>
        Task SoftDeleteAsync(int id, int? deletedBy, CancellationToken ct);
    }
}
