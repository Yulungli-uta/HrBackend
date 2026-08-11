using System.Data;
using WsUtaSystem.Application.DTOs;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IHrBalanceRepository
    {
        Task<SpResult> AccrueVacationAsync(int employeeId, DateOnly? asOfDate, string mode, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> AccrueVacationCTAsync(int employeeId, DateOnly? asOfDate, string mode, int? performedByEmpId, IDbTransaction? tx = null);
        Task<SpResult> AccrueVacationLOESAsync(int employeeId, DateOnly? asOfDate, string mode, int? performedByEmpId, IDbTransaction? tx = null);
        /// <summary>Obsoleto (migrado 2026-07-22): VacationsService ya no llama esto — usa
        /// IVacationBalanceAdjustmentService.ReserveAsync (EF Core), que además resuelve el
        /// régimen real del empleado en vez del "LOSEP siempre (57)" hardcodeado de esta SP
        /// (confirmado con prueba real: para empleados de Código de Trabajo/LOES, esta SP
        /// reportaba éxito pero nunca descontaba el saldo real).</summary>
        [Obsolete("Migrado a IVacationBalanceAdjustmentService.ReserveAsync (EF Core) — esta SP tenía el régimen 57 (LOSEP) hardcodeado.")]
        Task<SpResult> ReserveVacationAsync(int vacationId, int? performedByEmpId, IDbTransaction? tx = null);

        /// <summary>Obsoleto (migrado 2026-07-22): PermissionsService ya no llama esto — usa
        /// IVacationBalanceAdjustmentService.ReserveAsync (EF Core). Mismo motivo que
        /// ReserveVacationAsync (régimen 57 hardcodeado).</summary>
        [Obsolete("Migrado a IVacationBalanceAdjustmentService.ReserveAsync (EF Core) — esta SP tenía el régimen 57 (LOSEP) hardcodeado.")]
        Task<SpResult> ReservePermissionAsync(int permissionId, int? performedByEmpId, IDbTransaction? tx = null);

        /// <summary>Obsoleto (migrado 2026-07-22) — usa IVacationBalanceAdjustmentService.MarkReservationConsumedAsync (EF Core).</summary>
        [Obsolete("Migrado a IVacationBalanceAdjustmentService.MarkReservationConsumedAsync (EF Core).")]
        Task<SpResult> ConsumeReservationAsync(string reserveSourceId, int? performedByEmpId, IDbTransaction? tx = null);

        /// <summary>Obsoleto (migrado 2026-07-22) — usa IVacationBalanceAdjustmentService.ReleaseReservationAsync (EF Core).</summary>
        [Obsolete("Migrado a IVacationBalanceAdjustmentService.ReleaseReservationAsync (EF Core).")]
        Task<SpResult> ReleaseReservationAsync(string reserveSourceId, int? performedByEmpId, IDbTransaction? tx = null);
        /// <summary>Obsoleto y sin uso real (verificado 2026-07-22): ningún Service/Controller/Job
        /// invoca este método. El mecanismo vigente de recuperación de horas es
        /// HR.tbl_TimePlanning (PlanType='Recovery') + el pipeline diario de asistencia.</summary>
        [Obsolete("Sin uso real — ver HR.tbl_TimePlanning (PlanType='Recovery') y el pipeline diario de asistencia.")]
        Task<SpResult> ProcessRecoveryAsync(int employeeId, DateOnly startDate, DateOnly endDate, int? performedByEmpId, IDbTransaction? tx = null);

        /// <summary>Obsoleto y sin uso real (verificado 2026-07-22): ningún Service/Controller/Job
        /// invoca este método. La ejecución real ya se registra en HR.tbl_TimePlanningExecution.</summary>
        [Obsolete("Sin uso real — la ejecución real se registra en HR.tbl_TimePlanningExecution.")]
        Task<SpResult> DebitRecoveryAsync(int recoveryLogId, int? performedByEmpId, IDbTransaction? tx = null);

        Task<(EmployeeBalanceDto balance, IReadOnlyList<MovementDto> movements)> GetBalancesAsync(int employeeId);
    }
}
