
using WsUtaSystem.Application.DTOs;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{

    public sealed class HrBalanceService : IHrBalanceService
    {
        private readonly IHrBalanceRepository _repo;

        public HrBalanceService(IHrBalanceRepository repo)
        {
            _repo = repo;
        }

        public Task<SpResult> RunDailyAccrualAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId)
            => _repo.AccrueVacationAsync(employeeId, asOfDate, mode: "DAILY", performedByEmpId);

        public Task<SpResult> RunMonthlyAccrualAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId)
            => _repo.AccrueVacationAsync(employeeId, asOfDate, mode: "TOTAL", performedByEmpId);

        public Task<SpResult> RunMonthlyAccrualCTAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId)
            => _repo.AccrueVacationCTAsync(employeeId, asOfDate, mode: "MONTHLY", performedByEmpId);

        public Task<SpResult> RunMonthlyAccrualLOESAsync(int employeeId, DateOnly? asOfDate, int? performedByEmpId)
            => _repo.AccrueVacationLOESAsync(employeeId, asOfDate, mode: "MONTHLY", performedByEmpId);

#pragma warning disable CS0618 // wrappers obsoletos que llaman a SP también obsoletas — ver IHrBalanceService
        [Obsolete("Sin uso real — VacationsService usa IVacationBalanceAdjustmentService.ReserveAsync directamente.")]
        public Task<SpResult> ReserveVacationOnCreateAsync(int vacationId, int? performedByEmpId)
            => _repo.ReserveVacationAsync(vacationId, performedByEmpId);

        [Obsolete("Sin uso real — VacationsService usa IVacationBalanceAdjustmentService.MarkReservationConsumedAsync directamente.")]
        public async Task<SpResult> ApproveVacationAsync(int vacationId, int? performedByEmpId)
        {
            // Aquí asumes que ya actualizaste Status='Approved' en tu módulo de Vacaciones.
            // Luego consumes la reserva (audit).
            var sourceId = $"VAC_RESERVE|{vacationId}";
            return await _repo.ConsumeReservationAsync(sourceId, performedByEmpId);
        }

        [Obsolete("Sin uso real — VacationsService usa IVacationBalanceAdjustmentService.ReleaseReservationAsync directamente.")]
        public async Task<SpResult> RejectVacationAsync(int vacationId, int? performedByEmpId)
        {
            // Aquí asumes que actualizaste Status='Rejected' o 'Canceled'
            // Devuelves saldo
            var sourceId = $"VAC_RESERVE|{vacationId}";
            return await _repo.ReleaseReservationAsync(sourceId, performedByEmpId);
        }

        [Obsolete("Sin uso real — PermissionsService usa IVacationBalanceAdjustmentService.ReserveAsync directamente.")]
        public Task<SpResult> ReservePermissionOnCreateAsync(int permissionId, int? performedByEmpId)
            => _repo.ReservePermissionAsync(permissionId, performedByEmpId);

        [Obsolete("Sin uso real — PermissionsService usa IVacationBalanceAdjustmentService.MarkReservationConsumedAsync directamente.")]
        public Task<SpResult> ApprovePermissionAsync(int permissionId, int? performedByEmpId)
            => _repo.ConsumeReservationAsync($"PERM_RESERVE|{permissionId}", performedByEmpId);

        [Obsolete("Sin uso real — PermissionsService usa IVacationBalanceAdjustmentService.ReleaseReservationAsync directamente.")]
        public Task<SpResult> RejectPermissionAsync(int permissionId, int? performedByEmpId)
            => _repo.ReleaseReservationAsync($"PERM_RESERVE|{permissionId}", performedByEmpId);
#pragma warning restore CS0618

        public Task<(EmployeeBalanceDto, IReadOnlyList<MovementDto>)> GetBalancesAsync(int employeeId)
            => _repo.GetBalancesAsync(employeeId);

       
    }

}
