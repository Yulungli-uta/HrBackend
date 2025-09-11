using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class FacultiesService : Service<Faculties, int>, IFacultiesService
{
    public FacultiesService(IFacultiesRepository repo) : base(repo) { }
}
