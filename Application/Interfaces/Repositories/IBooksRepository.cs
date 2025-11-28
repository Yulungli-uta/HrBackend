using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IBooksRepository : IRepository<Books, int>
{
    Task<IEnumerable<Books>> GetByPersonIdAsync(int personId);
}
