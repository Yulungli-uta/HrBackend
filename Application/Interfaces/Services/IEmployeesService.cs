using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IEmployeesService : IService<Employees, int> 
{
    Task<IEnumerable<Employees>> GetSubordinatesByBossIdAsync(int bossId, CancellationToken ct = default);
}
