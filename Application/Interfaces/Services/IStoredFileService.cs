using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IStoredFileService : IService<StoredFile, int>
    {
        Task<IEnumerable<StoredFile>> GetByEntityAsync(
            string directoryCode,
            string entityType,
            string entityId,
            int? uploadYear,
            int? status,
            CancellationToken ct);

        Task<StoredFile?> GetByGuidAsync(Guid fileGuid, CancellationToken ct);

        Task SoftDeleteAsync(int id, int? deletedBy, CancellationToken ct);
    }
}
