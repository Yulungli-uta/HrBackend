using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IFamilyBurdenService : IService<FamilyBurden, int>
{
    Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId);
}
