using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface ITimePlanningEmployeeRepository : IRepository<TimePlanningEmployee, int>
    {
        Task<IEnumerable<TimePlanningEmployee>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningEmployee>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
        Task<TimePlanningEmployee?> GetByPlanAndEmployeeAsync(int planId, int employeeId, CancellationToken ct = default);
        Task<IEnumerable<TimePlanningEmployee>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
        Task<int> GetEmployeeCountByPlanAsync(int planId, CancellationToken ct = default);
        Task<bool> UpdateEmployeeStatusAsync(int planEmployeeId, int newStatusTypeId, CancellationToken ct = default);
        Task<bool> BulkUpdateStatusAsync(int planId, int newStatusTypeId, CancellationToken ct = default);

        /// <summary>
        /// Planificaciones (horas extra o recuperación) en las que el empleado ya está asignado y cuyo
        /// horario se cruza con el rango indicado. Ignora planes cancelados y asignaciones canceladas.
        /// Considera cada día del rango con su ventana horaria; un fin igual o menor al inicio (ej. 00:00)
        /// se interpreta como el día siguiente.
        /// </summary>
        Task<IReadOnlyList<TimePlanning>> FindOverlappingPlansAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Candado exclusivo de la regla de cruce, atado a la transacción actual: serializa las altas y
        /// ediciones de planificaciones para que dos usuarios simultáneos no crucen al mismo empleado.
        /// Devuelve false si no se obtuvo en el tiempo de espera.
        /// </summary>
        Task<bool> TryAcquireOverlapLockAsync(CancellationToken ct = default);

        /// <summary>Cabecera de la planificación sin seguimiento de cambios (null si no existe).</summary>
        Task<TimePlanning?> GetPlanHeaderAsync(int planId, CancellationToken ct = default);

        /// <summary>Empleados no cancelados asignados a la planificación.</summary>
        Task<IReadOnlyList<int>> GetActiveEmployeeIdsByPlanAsync(int planId, CancellationToken ct = default);

        /// <summary>Indica si la planificación está cancelada.</summary>
        Task<bool> IsPlanCancelledAsync(int planId, CancellationToken ct = default);

        /// <summary>Nombre completo del empleado para mensajes al usuario (null si no existe).</summary>
        Task<string?> GetEmployeeDisplayNameAsync(int employeeId, CancellationToken ct = default);

        Task<bool> ExistsOverlapAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default);
    }

}
