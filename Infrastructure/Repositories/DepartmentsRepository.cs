using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class DepartmentsRepository : ServiceAwareEfRepository<Departments, int>, IDepartmentsRepository
{
    public DepartmentsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
