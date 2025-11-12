using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IVacationsService : IService<Vacations, int> {
    Task<IEnumerable<Vacations>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<Vacations>> GetByImmediateBossId(int immediateBossId, CancellationToken ct);
}
