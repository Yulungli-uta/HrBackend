using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.DTOs.ContractStatusHistory;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Models;
using ContractDocumentStatusDto = WsUtaSystem.Application.DTOs.Contracts.ContractDocumentStatusDto;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IContractsService : IService<Contracts, int> {
    Task<Contracts> CreateAndNotifyAsync(Contracts entity, CancellationToken ct, bool isHistoricalEntry = false);
    Task UpdateAndNotifyAsync(int id, Contracts entity, CancellationToken ct);
    Task UpdateAsync(int id, ContractsUpdateDto dto, CancellationToken ct);

    /// <summary>Resuelve el PersonID del titular de un EmployeeId (para filtrar contratos por empleado).</summary>
    Task<int?> ResolvePersonIdByEmployeeIdAsync(int employeeId, CancellationToken ct);

    /// <summary>
    /// Nombre completo y cédula de las personas indicadas, en una sola consulta (para los listados de contratos).
    /// Las personas que no existan no aparecen en el resultado.
    /// </summary>
    Task<IReadOnlyDictionary<int, (string FullName, string IdCard)>> GetPersonSummariesAsync(IEnumerable<int> personIds, CancellationToken ct);

    /// <summary>
    /// PersonId de las personas cuya cédula o nombre contienen el texto buscado (mínimo 3 caracteres,
    /// tope de 500). Permite que el buscador de contratos encuentre por titular además de por código.
    /// </summary>
    Task<IReadOnlyList<int>> SearchPersonIdsByTextAsync(string term, CancellationToken ct);

    /// <summary>
    /// Corrige los datos de un contrato en CUALQUIER estado (a diferencia de
    /// <see cref="UpdateAsync(int, ContractsUpdateDto, CancellationToken)"/>, que solo permite
    /// BORRADOR/GENERADO). Exige motivo obligatorio y queda registrada en HR.Audit
    /// (Action=CORRECTION) con el detalle de los campos modificados.
    /// </summary>
    Task CorrectAsync(int id, ContractsUpdateDto dto, string reason, CancellationToken ct);

    Task<IReadOnlyList<int>> GetAllowedNextStatusesAsync(int currentStatusTypeId, CancellationToken ct);

    Task ChangeStatusAsync(int contractId, int toStatusTypeId, string? comment, CancellationToken ct);

    /// <summary>
    /// Corrige directamente el Status de un contrato a cualquier estado válido de
    /// HR.ref_Types/CONTRACT_STATUS, sin validar <c>HR.tbl_ContractStatusTransition</c> y sin
    /// disparar ningún efecto secundario (reversar cupo de solicitud, anular contrato padre,
    /// etc.) — esos efectos solo ocurren en el flujo normal (<see cref="ChangeStatusAsync"/>).
    /// Exige motivo obligatorio y queda registrada en el historial de estados.
    /// </summary>
    Task CorrectStatusAsync(int contractId, int toStatusTypeId, string reason, CancellationToken ct);

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
