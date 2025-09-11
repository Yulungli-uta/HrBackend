using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmployeeSchedulesRepository : ServiceAwareEfRepository<EmployeeSchedules, int>, IEmployeeSchedulesRepository
{
    public EmployeeSchedulesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
