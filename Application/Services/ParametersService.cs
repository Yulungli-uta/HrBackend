using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class ParametersService : Service<Parameters, int>, IParametersService
{
    public ParametersService(IParametersRepository repo) : base(repo) { }
}

