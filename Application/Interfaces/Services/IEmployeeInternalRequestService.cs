using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IEmployeeInternalRequestService
{
    Task<EmployeeInternalRequestDetailDto> CreateAsync(int employeeId, CreateEmployeeInternalRequest request, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> UpdateAsync(int requestId, int employeeId, UpdateEmployeeInternalRequest request, CancellationToken ct = default);

    Task CancelOwnAsync(int requestId, int employeeId, CancelEmployeeInternalRequest request, CancellationToken ct = default);

    Task<PagedEmployeeInternalRequestResult> GetMyRequestsAsync(int employeeId, EmployeeInternalRequestQueryFilter filter, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> GetMyRequestDetailAsync(int requestId, int employeeId, CancellationToken ct = default);

    Task<PagedEmployeeInternalRequestResult> GetPagedAsync(EmployeeInternalRequestQueryFilter filter, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> GetDetailByIdAsync(int requestId, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> ApproveAsync(int requestId, int reviewedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> RejectAsync(int requestId, int reviewedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> ReturnAsync(int requestId, int reviewedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> CompleteAsync(int requestId, int resolvedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default);

    Task<EmployeeInternalRequestDetailDto> HrCancelAsync(int requestId, int cancelledBy, CancelEmployeeInternalRequest request, CancellationToken ct = default);
}
