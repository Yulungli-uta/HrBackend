
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

        public Task<SpResult> ReserveVacationOnCreateAsync(int vacationId, int? performedByEmpId)
            => _repo.ReserveVacationAsync(vacationId, performedByEmpId);

        public async Task<SpResult> ApproveVacationAsync(int vacationId, int? performedByEmpId)
        {
            // Aquí asumes que ya actualizaste Status='Approved' en tu módulo de Vacaciones.
            // Luego consumes la reserva (audit).
            var sourceId = $"VAC_RESERVE|{vacationId}";
            return await _repo.ConsumeReservationAsync(sourceId, performedByEmpId);
        }

        public async Task<SpResult> RejectVacationAsync(int vacationId, int? performedByEmpId)
        {
            // Aquí asumes que actualizaste Status='Rejected' o 'Canceled'
            // Devuelves saldo
            var sourceId = $"VAC_RESERVE|{vacationId}";
            return await _repo.ReleaseReservationAsync(sourceId, performedByEmpId);
        }

        public Task<SpResult> ReservePermissionOnCreateAsync(int permissionId, int? performedByEmpId)
            => _repo.ReservePermissionAsync(permissionId, performedByEmpId);

        public Task<SpResult> ApprovePermissionAsync(int permissionId, int? performedByEmpId)
            => _repo.ConsumeReservationAsync($"PERM_RESERVE|{permissionId}", performedByEmpId);

        public Task<SpResult> RejectPermissionAsync(int permissionId, int? performedByEmpId)
            => _repo.ReleaseReservationAsync($"PERM_RESERVE|{permissionId}", performedByEmpId);

        public Task<(EmployeeBalanceDto, IReadOnlyList<MovementDto>)> GetBalancesAsync(int employeeId)
            => _repo.GetBalancesAsync(employeeId);

       
    }

}
