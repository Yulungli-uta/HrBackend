using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class ContractRequestRepository : ServiceAwareEfRepository<ContractRequest, int>, IContractRequestRepository
{
    public ContractRequestRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}

