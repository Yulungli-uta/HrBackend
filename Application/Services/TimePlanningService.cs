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

        public async Task<TimePlanning> CreateWithEmployeesAsync(TimePlanning entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            ValidateEntity(entity);

            var employees = entity.Employees?.ToList() ?? new List<TimePlanningEmployee>();

            if (!employees.Any())
                throw new InvalidOperationException("La planificación debe contener al menos un empleado.");

            ValidateDuplicatedEmployeesInRequest(employees);

            // Evita que EF Core inserte también el grafo de hijos al crear la cabecera
            entity.Employees = new List<TimePlanningEmployee>();

            var strategy = _db.Database.CreateExecutionStrategy();
            TimePlanning createdPlan = null!;

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted, ct);

                try
                {
                    createdPlan = await base.CreateAsync(entity, ct);

                    foreach (var emp in employees)
                    {
                        NormalizeEmployeeDetail(emp, createdPlan);

                        await ValidateEmployeeDetailAsync(createdPlan, emp, ct);

                        await _timePlanningEmployeeService.CreateAsync(emp, ct);
                    }

                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });

            return createdPlan;
        }

        private async Task ValidateEmployeeDetailAsync(
            TimePlanning plan,
            TimePlanningEmployee employee,
            CancellationToken ct)
        {
            if (employee.EmployeeID <= 0)
                throw new InvalidOperationException("EmployeeID inválido en detalle.");

            if (employee.EmployeeStatusTypeID <= 0)
                throw new InvalidOperationException(
                    $"EmployeeStatusTypeID inválido para EmployeeID={employee.EmployeeID}.");

            var isEligible = await _timePlanningEmployeeService.ValidateEmployeeEligibilityAsync(
                employee.EmployeeID,
                plan.StartDate,
                plan.EndDate,
                plan.StartTime,
                plan.EndTime,
                excludePlanId: null,
                ct);

            if (!isEligible)
            {
                throw new InvalidOperationException(
                    $"El empleado {employee.EmployeeID} ya tiene otra planificación en el mismo rango de fecha y hora.");
            }

            if (plan.PlanType == "Overtime")
            {
                if ((employee.AssignedHours ?? 0m) <= 0m)
                    throw new InvalidOperationException(
                        $"AssignedHours debe ser mayor a 0 para EmployeeID={employee.EmployeeID}.");

                employee.AssignedMinutes = null;
            }
            else if (plan.PlanType == "Recovery")
            {
                if ((employee.AssignedMinutes ?? 0) <= 0)
                    throw new InvalidOperationException(
                        $"AssignedMinutes debe ser mayor a 0 para EmployeeID={employee.EmployeeID}.");

                employee.AssignedHours = null;
            }
        }

        private static void NormalizeEmployeeDetail(TimePlanningEmployee employee, TimePlanning createdPlan)
        {
            employee.PlanEmployeeID = 0;
            employee.PlanID = createdPlan.PlanID;
            employee.TimePlanning = null;

            employee.ActualHours ??= 0m;
            employee.ActualMinutes ??= 0;
            employee.PaymentAmount ??= 0m;
            employee.IsEligible = true;
            employee.CreatedAt = DateTime.Now;
        }

        private static void ValidateDuplicatedEmployeesInRequest(IEnumerable<TimePlanningEmployee> employees)
        {
            var duplicateEmployees = employees
                .GroupBy(x => x.EmployeeID)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateEmployees.Any())
            {
                throw new InvalidOperationException(
                    $"Existen empleados repetidos en la misma solicitud: {string.Join(", ", duplicateEmployees)}");
            }
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