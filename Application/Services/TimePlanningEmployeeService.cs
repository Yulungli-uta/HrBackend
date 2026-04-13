using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class TimePlanningEmployeeService : Service<TimePlanningEmployee, int>, ITimePlanningEmployeeService
    {
        private readonly ITimePlanningEmployeeRepository _repository;

        public TimePlanningEmployeeService(ITimePlanningEmployeeRepository repo) : base(repo)
        {
            _repository = repo;
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
        {
            return await _repository.GetByPlanIdAsync(planId, ct);
        }

        public async Task<IEnumerable<TimePlanningEmployee>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)
        {
            return await _repository.GetByEmployeeIdAsync(employeeId, ct);
        }

        public async Task<TimePlanningEmployee?> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default)
        {
            return await _repository.GetByPlanAndEmployeeAsync(planId, employeeId, ct);
        }

        public async Task<TimePlanningEmployee> UpdateEmployeeStatusAsync(int planEmployeeId, int statusTypeId, CancellationToken ct = default)
        {
            var success = await _repository.UpdateEmployeeStatusAsync(planEmployeeId, statusTypeId, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo actualizar el estado del empleado");

            return await _repository.GetByIdAsync(planEmployeeId, ct);
        }

        public async Task<bool> BulkUpdateEmployeeStatusAsync(int planId, int statusTypeId, CancellationToken ct = default)
        {
            return await _repository.BulkUpdateStatusAsync(planId, statusTypeId, ct);
        }

        public async Task<bool> ValidateEmployeeEligibilityAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default)
        {
            if (employeeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(employeeId));

            if (endDate.Date < startDate.Date)
                throw new InvalidOperationException("El rango de fechas es inválido.");

            if (endTime <= startTime)
                throw new InvalidOperationException("El rango de horas es inválido.");

            var existsOverlap = await _repository.ExistsOverlapAsync(
                employeeId,
                startDate,
                endDate,
                startTime,
                endTime,
                excludePlanId,
                ct);

            return !existsOverlap;
        }
    }
}