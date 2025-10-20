using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IVwEmployeeScheduleAtDateRepository
{
    Task<List<VwEmployeeScheduleAtDate>> GetAllAsync(CancellationToken ct = default);
    Task<List<VwEmployeeScheduleAtDate>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<List<VwEmployeeScheduleAtDate>> GetByDateAsync(DateTime date, CancellationToken ct = default);
}

