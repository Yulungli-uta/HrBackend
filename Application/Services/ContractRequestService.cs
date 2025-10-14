using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class ContractRequestService : Service<ContractRequest, int>, IContractRequestService
{
    public ContractRequestService(IContractRequestRepository repo) : base(repo) { }
}

