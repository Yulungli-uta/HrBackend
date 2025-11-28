using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IFamilyBurdenRepository : IRepository<FamilyBurden, int>
{
    Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId);
}
