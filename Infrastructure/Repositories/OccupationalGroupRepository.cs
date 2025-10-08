using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class OccupationalGroupRepository : ServiceAwareEfRepository<OccupationalGroup, int>, IOccupationalGroupRepository
{
    public OccupationalGroupRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
