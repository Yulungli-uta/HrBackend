using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class CatastrophicIllnessesService : Service<CatastrophicIllnesses, int>, ICatastrophicIllnessesService
{
    public CatastrophicIllnessesService(ICatastrophicIllnessesRepository repo) : base(repo) { }
}
