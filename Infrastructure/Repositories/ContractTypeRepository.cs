using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class ContractTypeRepository : ServiceAwareEfRepository<ContractType, int>, IContractTypeRepository
{
    public ContractTypeRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
