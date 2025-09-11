using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class OvertimeRepository : ServiceAwareEfRepository<Overtime, int>, IOvertimeRepository
{
    public OvertimeRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
