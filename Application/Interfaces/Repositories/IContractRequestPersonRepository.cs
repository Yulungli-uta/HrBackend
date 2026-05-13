using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IContractRequestPersonRepository : IRepository<ContractRequestPerson, int>
{
    Task<IEnumerable<ContractRequestPerson>> GetByRequestAsync(int requestId, CancellationToken ct = default);
    Task<IEnumerable<ContractRequestPerson>> GetPendingByRequestAsync(int requestId, int pendingStatusId, CancellationToken ct = default);
}
