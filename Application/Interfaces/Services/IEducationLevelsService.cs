using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IEducationLevelsService : IService<EducationLevels, int>
{
    Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId);
}
