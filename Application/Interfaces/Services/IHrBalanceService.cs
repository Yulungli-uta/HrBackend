using WsUtaSystem.Application.DTOs;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IHrBalanceService
    {
        Task<SpResult> RunDailyAccrualAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId);
        Task<SpResult> RunMonthlyAccrualAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId);

        /// <summary>Acreditación mensual automática — régimen Código de Trabajo. Solo MONTHLY (sin TOTAL, ver sp_hr_AccrueVacationBalance_CT).</summary>
        Task<SpResult> RunMonthlyAccrualCTAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId);

        /// <summary>Acreditación mensual automática — régimen LOES.</summary>
        Task<SpResult> RunMonthlyAccrualLOESAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId);

        /// <summary>Obsoleto y sin uso real (verificado 2026-07-22): ningún Service/Controller
        /// invoca este wrapper — VacationsService reserva directo vía
        /// IVacationBalanceAdjustmentService.ReserveAsync (EF Core) desde 2026-07-22.</summary>
        [Obsolete("Sin uso real — VacationsService usa IVacationBalanceAdjustmentService.ReserveAsync directamente.")]
        Task<SpResult> ReserveVacationOnCreateAsync(int vacationId, int? performedByEmpId);
        [Obsolete("Sin uso real — VacationsService usa IVacationBalanceAdjustmentService.MarkReservationConsumedAsync directamente.")]
        Task<SpResult> ApproveVacationAsync(int vacationId, int? performedByEmpId);
        [Obsolete("Sin uso real — VacationsService usa IVacationBalanceAdjustmentService.ReleaseReservationAsync directamente.")]
        Task<SpResult> RejectVacationAsync(int vacationId, int? performedByEmpId);

        /// <summary>Obsoleto y sin uso real (verificado 2026-07-22): ningún Service/Controller
        /// invoca este wrapper — PermissionsService reserva directo vía
        /// IVacationBalanceAdjustmentService.ReserveAsync (EF Core) desde 2026-07-22.</summary>
        [Obsolete("Sin uso real — PermissionsService usa IVacationBalanceAdjustmentService.ReserveAsync directamente.")]
        Task<SpResult> ReservePermissionOnCreateAsync(int permissionId, int? performedByEmpId);
        [Obsolete("Sin uso real — PermissionsService usa IVacationBalanceAdjustmentService.MarkReservationConsumedAsync directamente.")]
        Task<SpResult> ApprovePermissionAsync(int permissionId, int? performedByEmpId);
        [Obsolete("Sin uso real — PermissionsService usa IVacationBalanceAdjustmentService.ReleaseReservationAsync directamente.")]
        Task<SpResult> RejectPermissionAsync(int permissionId, int? performedByEmpId);

        Task<(EmployeeBalanceDto, IReadOnlyList<MovementDto>)> GetBalancesAsync(int employeeId);
    }
}



