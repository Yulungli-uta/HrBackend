using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IContractRequestRepository : IRepository<ContractRequest, int>
{
    Task<IEnumerable<ContractRequest>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
    Task IncrementTotalHiredAsync(int requestId, CancellationToken ct = default);
    Task UpdateStatusAsync(int requestId, int statusTypeId, CancellationToken ct = default);
}
