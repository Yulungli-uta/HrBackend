using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class StoredFileService : Service<StoredFile, int>, IStoredFileService
    {
        private readonly IStoredFileRepository _repository;
        private readonly ILogger<StoredFileService> _logger;

        public StoredFileService(
            IStoredFileRepository repo,
            ILogger<StoredFileService> logger
        ) : base(repo)
        {
            _repository = repo;
            _logger = logger;
        }

        public async Task<IEnumerable<StoredFile>> GetByEntityAsync(
            string directoryCode,
            string entityType,
            string entityId,
            int? uploadYear,
            int? status,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "Listing files: DirectoryCode={DirectoryCode}, EntityType={EntityType}, EntityId={EntityId}, UploadYear={UploadYear}, Status={Status}",
                directoryCode, entityType, entityId, uploadYear, status);

            var result = await _repository.GetByEntityAsync(directoryCode, entityType, entityId, uploadYear, status, ct);

            // Ojo: no loguear listas completas; solo conteos.
            _logger.LogInformation("Listing files result: Count={Count}", result.Count());

            return result;
        }

        public Task<StoredFile?> GetByGuidAsync(Guid fileGuid, CancellationToken ct)
            => _repository.GetByGuidAsync(fileGuid, ct);

        public async Task SoftDeleteAsync(int id, int? deletedBy, CancellationToken ct)
        {
            _logger.LogWarning("Soft delete file requested: FileId={FileId}, DeletedBy={DeletedBy}", id, deletedBy);

            await _repository.SoftDeleteAsync(id, deletedBy, ct);

            _logger.LogWarning("Soft delete file completed: FileId={FileId}", id);
        }
    }
}
