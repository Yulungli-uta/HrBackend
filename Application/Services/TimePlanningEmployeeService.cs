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

        public async Task<TimePlanningEmployee> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default)
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

        public async Task<bool> ValidateEmployeeEligibilityAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            // Implementar lógica de validación según requerimientos
            return true;
        }
    }
}
