using System.Data;
using WsUtaSystem.Application.DTOs;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IHrBalanceRepository
    {
        Task<SpResult> AccrueVacationAsync(int employeeId, DateOnly? asOfDate, string mode, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> ReserveVacationAsync(int vacationId, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> ReservePermissionAsync(int permissionId, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> ConsumeReservationAsync(string reserveSourceId, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> ReleaseReservationAsync(string reserveSourceId, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> ProcessRecoveryAsync(int employeeId, DateOnly startDate, DateOnly endDate, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> DebitRecoveryAsync(int recoveryLogId, int? performedByEmpId, IDbTransaction? tx = null);

        Task<(EmployeeBalanceDto balance, IReadOnlyList<MovementDto> movements)> GetBalancesAsync(int employeeId);
    }
}
