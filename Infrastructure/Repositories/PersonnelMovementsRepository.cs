using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PersonnelMovementsRepository : ServiceAwareEfRepository<PersonnelMovements, int>, IPersonnelMovementsRepository
{
    public PersonnelMovementsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
