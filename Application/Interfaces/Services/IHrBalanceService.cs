using WsUtaSystem.Application.DTOs;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IHrBalanceService
    {
        Task<SpResult> RunDailyAccrualAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId);
        Task<SpResult> RunMonthlyAccrualAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId);

        Task<SpResult> ReserveVacationOnCreateAsync(int vacationId, int? performedByEmpId);
        Task<SpResult> ApproveVacationAsync(int vacationId, int? performedByEmpId);
        Task<SpResult> RejectVacationAsync(int vacationId, int? performedByEmpId);

        Task<SpResult> ReservePermissionOnCreateAsync(int permissionId, int? performedByEmpId);
        Task<SpResult> ApprovePermissionAsync(int permissionId, int? performedByEmpId);
        Task<SpResult> RejectPermissionAsync(int permissionId, int? performedByEmpId);

        Task<(EmployeeBalanceDto, IReadOnlyList<MovementDto>)> GetBalancesAsync(int employeeId);
    }
}



