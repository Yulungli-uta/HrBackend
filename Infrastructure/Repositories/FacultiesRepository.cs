using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class FacultiesRepository : ServiceAwareEfRepository<Faculties, int>, IFacultiesRepository
{
    public FacultiesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
