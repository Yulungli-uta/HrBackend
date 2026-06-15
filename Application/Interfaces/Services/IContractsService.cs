using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.DTOs.ContractStatusHistory;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Models;
using ContractDocumentStatusDto = WsUtaSystem.Application.DTOs.Contracts.ContractDocumentStatusDto;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IContractsService : IService<Contracts, int> {
    Task<Contracts> CreateAndNotifyAsync(Contracts entity, CancellationToken ct);
    Task UpdateAndNotifyAsync(int id, Contracts entity, CancellationToken ct);
    Task UpdateAsync(int id, ContractsUpdateDto dto, CancellationToken ct);

    Task<IReadOnlyList<int>> GetAllowedNextStatusesAsync(int currentStatusTypeId, CancellationToken ct);

    Task ChangeStatusAsync(int contractId, int toStatusTypeId, string? comment, CancellationToken ct);

    Task<IReadOnlyList<ContractStatusHistoryDto>> GetStatusHistoryAsync(int contractId, CancellationToken ct);

    Task<IReadOnlyList<Contracts>> GetAddendumsAsync(int contractId, CancellationToken ct);

    /// <summary>Vincula un documento generado al contrato y lo marca como congelado.</summary>
    Task FreezeDocumentAsync(int contractId, int documentId, int templateVersion, CancellationToken ct);

    /// <summary>Descongela el documento permitiendo regenerar el PDF del contrato.</summary>
    Task UnfreezeDocumentAsync(int contractId, CancellationToken ct);

    /// <summary>Obtiene el estado del documento asociado a un contrato.</summary>
    Task<ContractDocumentStatusDto?> GetDocumentStatusAsync(int contractId, CancellationToken ct);

    Task<GenerateContractDocumentResponse> GenerateDocumentAsync(int contractId, GenerateContractDocumentRequest request, int generatedBy, CancellationToken ct);

    Task MarkDocumentPendingSignaturesAsync(int contractId, string? comment, int updatedBy, CancellationToken ct);
    Task UploadSignedDocumentAsync(int contractId, UploadSignedContractDocumentRequest request, int updatedBy, CancellationToken ct);
    Task FinalizeDocumentAsync(int contractId, string? comment, int updatedBy, CancellationToken ct);
    Task CancelDocumentAsync(int contractId, CancelContractDocumentRequest request, int updatedBy, CancellationToken ct);

    /// <summary>
    /// Valida que se puede crear un contrato raíz: certificación aprobada y cupo disponible.
    /// Lanza InvalidOperationException si no se cumplen las condiciones.
    /// </summary>
    Task ValidateCanCreateContractAsync(int? certificationId, CancellationToken ct = default);

    /// <summary>
    /// Retorna contratos para reporte aplicando filtros de fecha de inicio, dependencia y estado.
    /// Incluye persona, tipo de contrato, régimen laboral y modalidad de trabajo.
    /// </summary>
    Task<IReadOnlyList<ContractReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default);
}
