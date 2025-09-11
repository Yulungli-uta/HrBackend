using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmployeesRepository : ServiceAwareEfRepository<Employees, int>, IEmployeesRepository
{
    public EmployeesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
