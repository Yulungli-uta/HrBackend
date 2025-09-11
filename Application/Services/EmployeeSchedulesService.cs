using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EmployeeSchedulesService : Service<EmployeeSchedules, int>, IEmployeeSchedulesService
{
    public EmployeeSchedulesService(IEmployeeSchedulesRepository repo) : base(repo) { }
}
