using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class OccupationalGroupService : Service<OccupationalGroup, int>, IOccupationalGroupService
{
    public OccupationalGroupService(IOccupationalGroupRepository repo) : base(repo) { }
}
