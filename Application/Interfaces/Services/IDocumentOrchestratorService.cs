using WsUtaSystem.Application.DTOs.Documents;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IDocumentOrchestratorService
    {
        Task<DocumentUploadResultDto> UploadAndRegisterAsync(DocumentUploadRequestDto request, CancellationToken ct);

        Task<DocumentUploadResultDto> UploadSingleAndRegisterAsync(DocumentUploadSingleRequestDto request, CancellationToken ct);
        Task<DocumentUploadResultDto> UploadMappedAndRegisterAsync(DocumentUploadMappedRequestDto request, CancellationToken ct);


        Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadByGuidAsync(Guid fileGuid, CancellationToken ct);

        Task<List<WsUtaSystem.Models.StoredFile>> ListByEntityAsync(
            string directoryCode,
            string entityType,
            string entityId,
            int? uploadYear,
            int? status,
            CancellationToken ct);

        /// <summary>
        /// Soft delete en DB y opcionalmente borrar físico.
        /// </summary>
        Task<bool> DeleteByGuidAsync(Guid fileGuid, bool deletePhysical, int? deletedBy, CancellationToken ct);
    }
}
