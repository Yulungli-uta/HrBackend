using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IEmployeeSchedulesService : IService<EmployeeSchedules, int> {


    Task<IEnumerable<EmployeeSchedules>> FindByEmployeeIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<EmployeeSchedules>> UpdateEmployeeScheduler(EmployeeSchedules employeeSchedules, CancellationToken ct);

    /// <summary>
    /// Igual que UpdateEmployeeScheduler, pero cuando el horario es un caso
    /// especial (sustituto/maternidad/lactancia/otro): crea primero la fila
    /// en HR.tbl_EmployeeSpecialSchedules y enlaza la nueva fila de
    /// EmployeeSchedules a ella (ScheduleId queda NULL).
    /// </summary>
    Task<IEnumerable<EmployeeSchedules>> UpdateEmployeeSchedulerWithSpecial(
        EmployeeSchedules employeeSchedules, EmployeeSpecialSchedule specialSchedule, CancellationToken ct);
}
