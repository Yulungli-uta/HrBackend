using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.TimePlanning;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class TimePlanningService : Service<TimePlanning, int>, ITimePlanningService
    {
        private readonly ITimePlanningRepository _repository;
        private readonly ITimePlanningEmployeeService _timePlanningEmployeeService;
        private readonly AppDbContext _db;

        public TimePlanningService(
            ITimePlanningRepository repo,
            ITimePlanningEmployeeService timePlanningEmployeeService,
            AppDbContext db
        ) : base(repo)
        {
            _repository = repo;
            _timePlanningEmployeeService = timePlanningEmployeeService;
            _db = db;
        }

        public async Task<IEnumerable<TimePlanning>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await _repository.GetByEmployeeAsync(employeeId, ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await _repository.GetByStatusAsync(statusTypeId, ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByCreateBy(int createBy, CancellationToken ct = default)
        {
            return await _repository.GetByCreateBy(createBy, ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _repository.GetByDateRangeAsync(startDate, endDate, ct);
        }

        public async Task<TimePlanning> SubmitForApprovalAsync(int planId, int submittedBy, CancellationToken ct = default)
        {
            var success = await _repository.ChangeStatusAsync(planId, 2, submittedBy, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo enviar la planificación para aprobación");

            return await _repository.GetByIdAsync(planId, ct);
        }

        public async Task<TimePlanning> ApprovePlanningAsync(int planId, int approvedBy, int? secondApprover = null, CancellationToken ct = default)
        {
            var success = await _repository.ChangeStatusAsync(planId, 3, approvedBy, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo aprobar la planificación");

            return await _repository.GetByIdAsync(planId, ct);
        }

        public async Task<TimePlanning> RejectPlanningAsync(int planId, int rejectedBy, string reason, CancellationToken ct = default)
        {
            var success = await _repository.ChangeStatusAsync(planId, 4, rejectedBy, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo rechazar la planificación");

            return await _repository.GetByIdAsync(planId, ct);
        }

        public async Task<bool> ValidatePlanningAsync(TimePlanningCreateDTO createDto, CancellationToken ct = default)
        {
            if (createDto == null)
                return false;

            if (string.IsNullOrWhiteSpace(createDto.PlanType))
                return false;

            if (createDto.PlanType != "Overtime" && createDto.PlanType != "Recovery")
                return false;

            if (string.IsNullOrWhiteSpace(createDto.Title))
                return false;

            if (createDto.EndDate.Date < createDto.StartDate.Date)
                return false;

            if (createDto.EndTime <= createDto.StartTime)
                return false;

            if (createDto.CreatedBy <= 0)
                return false;

            if (createDto.PlanStatusTypeID <= 0)
                return false;

            if (createDto.PlanType == "Overtime")
            {
                if (string.IsNullOrWhiteSpace(createDto.OvertimeType))
                    return false;

                if (createDto.Factor is null || createDto.Factor <= 0)
                    return false;
            }

            if (createDto.PlanType == "Recovery")
            {
                if (createDto.OwedMinutes is null || createDto.OwedMinutes <= 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Crea la cabecera de planificación y sus empleados en una única transacción
        /// compatible con <see cref="SqlServerRetryingExecutionStrategy"/>.
        ///
        /// <para>
        /// <b>¿Por qué no usar <c>TransactionScope</c>?</b><br/>
        /// EF Core con <c>SqlServerRetryingExecutionStrategy</c> (resilience/retry)
        /// no permite transacciones iniciadas por el usuario mediante <c>TransactionScope</c>
        /// porque la estrategia de reintento necesita controlar el ciclo de vida completo
        /// de la operación. La solución canónica es envolver todo en
        /// <c>Database.CreateExecutionStrategy().ExecuteAsync()</c> y usar
        /// <c>Database.BeginTransactionAsync()</c> dentro del delegate.
        /// </para>
        /// </summary>
        public async Task<TimePlanning> CreateWithEmployeesAsync(TimePlanning entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            ValidateEntity(entity);

            // IExecutionStrategy obtenido del DbContext — compatible con SqlServerRetryingExecutionStrategy
            var strategy = _db.Database.CreateExecutionStrategy();

            TimePlanning createdPlan = null!;

            await strategy.ExecuteAsync(async () =>
            {
                // Transacción EF Core — NO usar TransactionScope externo
                await using var transaction = await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted, ct);

                try
                {
                    // 1. Crear cabecera
                    createdPlan = await base.CreateAsync(entity, ct);

                    // 2. Crear empleados hijos asignando el PlanID recién generado
                    if (entity.Employees != null && entity.Employees.Any())
                    {
                        foreach (var emp in entity.Employees)
                        {
                            // Sobreescribir PlanID con el ID real creado en el paso anterior
                            emp.PlanID = createdPlan.PlanID;

                            if (emp.EmployeeID <= 0)
                                throw new InvalidOperationException("EmployeeID inválido en detalle.");

                            if (emp.EmployeeStatusTypeID <= 0)
                                throw new InvalidOperationException(
                                    $"EmployeeStatusTypeID inválido para EmployeeID={emp.EmployeeID}.");

                            if (createdPlan.PlanType == "Overtime")
                            {
                                if ((emp.AssignedHours ?? 0) <= 0)
                                    throw new InvalidOperationException(
                                        $"AssignedHours debe ser mayor a 0 para EmployeeID={emp.EmployeeID}.");

                                emp.AssignedMinutes = null;
                            }
                            else if (createdPlan.PlanType == "Recovery")
                            {
                                if ((emp.AssignedMinutes ?? 0) <= 0)
                                    throw new InvalidOperationException(
                                        $"AssignedMinutes debe ser mayor a 0 para EmployeeID={emp.EmployeeID}.");

                                emp.AssignedHours = null;
                            }

                            emp.ActualHours ??= 0;
                            emp.ActualMinutes ??= 0;
                            emp.IsEligible = true;

                            await _timePlanningEmployeeService.CreateAsync(emp, ct);
                        }
                    }

                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    // Rollback explícito ante cualquier fallo — EF Core no lo hace automáticamente
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });

            return createdPlan;
        }

        private static void ValidateEntity(TimePlanning entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PlanType))
                throw new InvalidOperationException("El tipo de planificación es requerido.");

            if (entity.PlanType != "Overtime" && entity.PlanType != "Recovery")
                throw new InvalidOperationException("PlanType debe ser 'Overtime' o 'Recovery'.");

            if (string.IsNullOrWhiteSpace(entity.Title))
                throw new InvalidOperationException("El título es requerido.");

            if (entity.EndDate.Date < entity.StartDate.Date)
                throw new InvalidOperationException("La fecha fin no puede ser menor a la fecha inicio.");

            if (entity.EndTime <= entity.StartTime)
                throw new InvalidOperationException("La hora fin debe ser mayor a la hora inicio.");

            if ((entity.CreatedBy ?? 0) <= 0)
                throw new InvalidOperationException("CreatedBy es obligatorio.");

            if (entity.PlanStatusTypeID <= 0)
                throw new InvalidOperationException("PlanStatusTypeID es obligatorio.");

            if (entity.PlanType == "Overtime")
            {
                if (string.IsNullOrWhiteSpace(entity.OvertimeType))
                    throw new InvalidOperationException("OvertimeType es obligatorio para horas extra.");

                if (entity.Factor is null || entity.Factor <= 0)
                    throw new InvalidOperationException("Factor es obligatorio para horas extra.");
            }

            if (entity.PlanType == "Recovery")
            {
                if (entity.OwedMinutes is null || entity.OwedMinutes <= 0)
                    throw new InvalidOperationException("OwedMinutes es obligatorio para recuperación.");
            }
        }
    }
}