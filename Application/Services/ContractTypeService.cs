using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class ContractTypeService : Service<ContractType, int>, IContractTypeService
{
    public ContractTypeService(IContractTypeRepository repo) : base(repo) { }
}
