using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IVacationsRepository : IRepository<Vacations, int> {
    Task<IEnumerable<Vacations>> GetByEmployeeId(int EmployeeId, CancellationToken ct);

}
