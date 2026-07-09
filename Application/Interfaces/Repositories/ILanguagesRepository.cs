using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface ILanguagesRepository : IRepository<Languages, int>
{
    Task<IEnumerable<Languages>> GetByPersonIdAsync(int personId);
}
