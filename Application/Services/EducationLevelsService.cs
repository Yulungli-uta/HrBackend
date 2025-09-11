using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EducationLevelsService : Service<EducationLevels, int>, IEducationLevelsService
{
    public EducationLevelsService(IEducationLevelsRepository repo) : base(repo) { }
}
