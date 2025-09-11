using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class AuditRepository : ServiceAwareEfRepository<Audit, long>, IAuditRepository
{
    public AuditRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
