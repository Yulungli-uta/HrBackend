using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class DegreeService : Service<Degree, int>, IDegreeService
{
    public DegreeService(IDegreeRepository repo) : base(repo) { }
}
