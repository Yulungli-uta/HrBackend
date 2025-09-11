using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class FamilyBurdenRepository : ServiceAwareEfRepository<FamilyBurden, int>, IFamilyBurdenRepository
{
    public FamilyBurdenRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
