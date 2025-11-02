using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IEmployeeSchedulesRepository : IRepository<EmployeeSchedules, int> {

    Task<IEnumerable<EmployeeSchedules>> findByEmployeeID(int id, CancellationToken ct);
    Task<IEnumerable<EmployeeSchedules>> UpdateEmployeeScheduler(EmployeeSchedules employeeSchedules, CancellationToken ct);

}
