using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class ContractsRepository : ServiceAwareEfRepository<Contracts, int>, IContractsRepository
{
    public ContractsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
