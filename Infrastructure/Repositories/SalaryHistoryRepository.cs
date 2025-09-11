using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class SalaryHistoryRepository : ServiceAwareEfRepository<SalaryHistory, int>, ISalaryHistoryRepository
{
    public SalaryHistoryRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
