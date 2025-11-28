using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPublicationsService : IService<Publications, int>
{
    Task<IEnumerable<Publications>> GetByPersonIdAsync(int personId);
}
