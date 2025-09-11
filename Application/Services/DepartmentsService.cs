using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class DepartmentsService : Service<Departments, int>, IDepartmentsService
{
    public DepartmentsService(IDepartmentsRepository repo) : base(repo) { }
}
