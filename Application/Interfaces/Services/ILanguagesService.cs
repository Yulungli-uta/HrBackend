using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ILanguagesService : IService<Languages, int>
{
    Task<IEnumerable<Languages>> GetByPersonIdAsync(int personId);
}
