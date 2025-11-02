using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IEmployeeSchedulesService : IService<EmployeeSchedules, int> {


    Task<IEnumerable<EmployeeSchedules>> FindByEmployeeIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<EmployeeSchedules>> UpdateEmployeeScheduler(EmployeeSchedules employeeSchedules, CancellationToken ct);
}
