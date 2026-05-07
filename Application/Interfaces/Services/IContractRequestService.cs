using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IContractRequestService : IService<ContractRequest, int>
{
    Task<IEnumerable<ContractRequestDto>> GetByStatusAsync(string statusName, CancellationToken ct = default);
    Task<PagedContractRequestResult> GetPagedAsync(ContractRequestQueryFilter filter, CancellationToken ct = default);
    Task<int> GetPendingCountAsync(int requestId, CancellationToken ct = default);
    Task IncrementTotalHiredAsync(int requestId, CancellationToken ct = default);
}
