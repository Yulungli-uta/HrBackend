using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IEmployeeInternalRequestRepository
{
    Task<EmployeeInternalRequest?> GetTrackedByIdAsync(int requestId, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto?> GetDetailByIdAsync(int requestId, CancellationToken ct = default);

    Task<PagedEmployeeInternalRequestResult> GetPagedAsync(EmployeeInternalRequestQueryFilter filter, CancellationToken ct = default);

    Task AddAsync(EmployeeInternalRequest entity, CancellationToken ct = default);

    Task AddHistoryAsync(EmployeeInternalRequestStatusHistory history, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
