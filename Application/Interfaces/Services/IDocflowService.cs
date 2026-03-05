using System.Diagnostics;
using WsUtaSystem.Application.DTOs.Docflow;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IDocflowService
    {
        // --- GRUPO 1: CONFIGURACIÓN DE PROCESOS (DEFINICIÓN) ---
        Task<IReadOnlyList<ProcessDto>> GetProcessesAsync(CancellationToken ct);
        Task<IReadOnlyList<ProcessDto>> GetProcessByIdAsync(int processId, CancellationToken ct);
        Task<ProcessDto> CreateProcessAsync(CreateProcessRequestDto req, CancellationToken ct);
        Task<ProcessDto> UpdateProcessAsync(int processId, UpdateProcessRequestDto req, CancellationToken ct);
        Task DeleteProcessAsync(int processId, CancellationToken ct);

        // --- GRUPO 2: METADATOS Y CAMPOS DINÁMICOS ---
        Task<ProcessDynamicFieldsDto> GetProcessDynamicFieldsAsync(int processId, CancellationToken ct);
        Task UpdateProcessDynamicFieldsAsync(int processId, UpdateProcessDynamicFieldsRequest req, CancellationToken ct);

        // --- GRUPO 3: REGLAS DE DOCUMENTACIÓN (DOCUMENT RULES) ---
        Task<IReadOnlyList<DocumentRuleDto>> GetRuleAsync(CancellationToken ct);
        Task<IReadOnlyList<DocumentRuleDto>> GetRulesByProcessAsync(int processId, CancellationToken ct);
        Task<DocumentRuleDto> CreateRuleAsync(int processId, CreateDocumentRuleRequestDto req, CancellationToken ct);
        Task<DocumentRuleDto> UpdateRuleAsync(int ruleId, UpdateDocumentRuleRequestDto req, CancellationToken ct);
        Task DeleteRuleAsync(int ruleId, CancellationToken ct);

        // --- GRUPO 4: GESTIÓN DE EXPEDIENTES (INSTANCES) ---
        Task<InstanceDetailDto> CreateInstanceAsync(CreateInstanceRequest req, CancellationToken ct);
        Task<InstanceDetailDto> GetInstanceAsync(Guid instanceId, CancellationToken ct);
        Task<PagedResultDto<InstanceListItemDto>> SearchInstancesAsync(
            int page, int pageSize, string? status, int? processId, string? q, DateTime? from, DateTime? to,
            CancellationToken ct);

        // --- GRUPO 5: DOCUMENTOS Y ARCHIVOS (VERSIONAMIENTO) ---
        Task<IReadOnlyList<DocumentDto>> GetInstanceDocumentsAsync(Guid instanceId, CancellationToken ct);
        Task<DocumentDto> CreateDocumentAsync(Guid instanceId, CreateDocumentRequest req, CancellationToken ct);
        Task<IReadOnlyList<FileVersionDto>> GetDocumentVersionsAsync(Guid documentId, CancellationToken ct);
        Task<FileVersionDto> UploadDocumentVersionAsync(Guid documentId, IFormFile file, CancellationToken ct);
        Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadVersionAsync(Guid versionId, CancellationToken ct);

        // --- GRUPO 6: FLUJO Y MOVIMIENTOS (WORKFLOW) ---
        Task CreateMovementAsync(Guid instanceId, CreateMovementRequest req, CancellationToken ct);

        // --- GRUPO 7: REPORTES Y AUDITORÍA ---
        Task<IReadOnlyList<object>> GetReturnsAuditAsync(DateTime? from, DateTime? to, int? processId, int? userId, CancellationToken ct);
    }
}
