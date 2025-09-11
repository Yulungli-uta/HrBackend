using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EmployeesService : Service<Employees, int>, IEmployeesService
{
    public EmployeesService(IEmployeesRepository repo) : base(repo) { }
}
