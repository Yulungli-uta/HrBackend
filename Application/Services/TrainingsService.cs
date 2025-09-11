using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class TrainingsService : Service<Trainings, int>, ITrainingsService
{
    public TrainingsService(ITrainingsRepository repo) : base(repo) { }
}
