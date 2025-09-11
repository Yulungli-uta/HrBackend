using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class VacationsService : Service<Vacations, int>, IVacationsService
{
    public VacationsService(IVacationsRepository repo) : base(repo) { }
}
