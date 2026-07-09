using WsUtaSystem.Application.DTOs.ResignationRetirement;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IResignationRetirementRepository
{
    /// <summary>Información consolidada del empleado (personal, laboral, contractual, vacaciones, tiempo de servicio).</summary>
    Task<EmployeeConsolidatedInfoDto?> GetEmployeeConsolidatedInfoAsync(int employeeId, CancellationToken ct = default);

    Task<int?> GetPublishedTemplateIdAsync(string templateCode, CancellationToken ct = default);

    /// <summary>True si el empleado tiene una solicitud activa (PENDIENTE/EN_REVISION/DEVUELTO) del mismo tipo.</summary>
    Task<bool> HasActiveRequestAsync(int employeeId, string requestType, int? excludeRequestId = null, CancellationToken ct = default);

    /// <summary>Entidad rastreada por EF, para mutar estado dentro de una transacción del servicio.</summary>
    Task<ResignationRetirementRequest?> GetTrackedByIdAsync(int requestId, CancellationToken ct = default);

    Task<ResignationRetirementDetailDto?> GetDetailByIdAsync(int requestId, CancellationToken ct = default);

    Task<PagedResignationRetirementResult> GetPagedAsync(ResignationRetirementQueryFilter filter, CancellationToken ct = default);

    Task<IReadOnlyList<ResignationRetirementStatusHistoryDto>> GetHistoryAsync(int requestId, CancellationToken ct = default);

    Task AddAsync(ResignationRetirementRequest entity, CancellationToken ct = default);

    Task AddHistoryAsync(ResignationRetirementStatusHistory history, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
