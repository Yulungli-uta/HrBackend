using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PermissionsRepository : ServiceAwareEfRepository<Permissions, int>, IPermissionsRepository
{
    public PermissionsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
