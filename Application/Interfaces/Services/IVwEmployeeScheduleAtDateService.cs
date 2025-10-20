using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IVwEmployeeScheduleAtDateService
{
    Task<List<VwEmployeeScheduleAtDate>> GetAllAsync(CancellationToken ct = default);
    Task<List<VwEmployeeScheduleAtDate>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<List<VwEmployeeScheduleAtDate>> GetByDateAsync(DateTime date, CancellationToken ct = default);
}

