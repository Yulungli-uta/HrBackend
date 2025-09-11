using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PermissionTypesRepository : ServiceAwareEfRepository<PermissionTypes, int>, IPermissionTypesRepository
{
    public PermissionTypesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
