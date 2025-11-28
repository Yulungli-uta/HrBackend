using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IBooksService : IService<Books, int>
{
    Task<IEnumerable<Books>> GetByPersonIdAsync(int personId);
}
