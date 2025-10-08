using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class DegreeRepository : ServiceAwareEfRepository<Degree, int>, IDegreeRepository
{
    public DegreeRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
