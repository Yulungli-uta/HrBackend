using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class OvertimeConfigRepository : ServiceAwareEfRepository<OvertimeConfig, int>, IOvertimeConfigRepository
{
    public OvertimeConfigRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
