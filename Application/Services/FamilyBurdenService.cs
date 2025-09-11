using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class FamilyBurdenService : Service<FamilyBurden, int>, IFamilyBurdenService
{
    public FamilyBurdenService(IFamilyBurdenRepository repo) : base(repo) { }
}
