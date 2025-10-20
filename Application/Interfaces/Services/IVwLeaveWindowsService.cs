using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IVwLeaveWindowsService
{
    Task<List<VwLeaveWindows>> GetAllAsync(CancellationToken ct = default);
    Task<List<VwLeaveWindows>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<List<VwLeaveWindows>> GetByLeaveTypeAsync(string leaveType, CancellationToken ct = default);
}

