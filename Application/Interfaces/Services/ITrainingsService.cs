using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ITrainingsService : IService<Trainings, int>
{
    Task<IEnumerable<Trainings>> GetByPersonIdAsync(int personId);
}
