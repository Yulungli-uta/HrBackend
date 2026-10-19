using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface ITimePlanningEmployeeService : IService<TimePlanningEmployee, int>
    {
        Task<IEnumerable<TimePlanningEmployee>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningEmployee>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
        Task<TimePlanningEmployee?> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default);
        Task<TimePlanningEmployee> UpdateEmployeeStatusAsync(int planEmployeeId, int statusTypeId, CancellationToken ct = default);
        Task<bool> BulkUpdateEmployeeStatusAsync(int planId, int statusTypeId, CancellationToken ct = default);

        /// <summary>
        /// Garantiza que el empleado no esté en otra planificación (horas extra o recuperación) cuyo
        /// horario se cruce con el indicado. Lanza <see cref="Common.BusinessRuleException"/> con el
        /// detalle del cruce (plan, tipo, fechas y horas) para mostrarlo al usuario.
        /// </summary>
        Task EnsureNoOverlapAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Valida que, con las nuevas fechas u horas del plan, ninguno de sus empleados quede cruzado
        /// con otra planificación. No aplica si el plan está cancelado.
        /// </summary>
        Task EnsurePlanScheduleHasNoOverlapAsync(
            int planId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            CancellationToken ct = default);

        /// <summary>Toma el candado de la regla de cruce (dura lo que dure la transacción actual).</summary>
        Task AcquireOverlapLockAsync(CancellationToken ct = default);

        Task<bool> ValidateEmployeeEligibilityAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default);
    }
}
