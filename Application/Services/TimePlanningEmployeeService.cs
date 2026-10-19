using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class TimePlanningEmployeeService : Service<TimePlanningEmployee, int>, ITimePlanningEmployeeService
    {
        private readonly ITimePlanningEmployeeRepository _repository;
        private readonly AppDbContext _db;

        public TimePlanningEmployeeService(ITimePlanningEmployeeRepository repo, AppDbContext db) : base(repo)
        {
            _repository = repo;
            _db = db;
        }

        /// <summary>
        /// Agregar un empleado a una planificación ya creada: valida que no se cruce con otra
        /// planificación suya. Dentro de una transacción ajena (creación del plan con sus empleados)
        /// la validación ya la hizo quien la abrió.
        /// </summary>
        public override async Task<TimePlanningEmployee> CreateAsync(TimePlanningEmployee entity, CancellationToken ct)
        {
            if (_db.Database.CurrentTransaction is not null)
                return await base.CreateAsync(entity, ct);

            var strategy = _db.Database.CreateExecutionStrategy();
            TimePlanningEmployee created = null!;

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted, ct);

                await AcquireOverlapLockAsync(ct);

                var plan = await _repository.GetPlanHeaderAsync(entity.PlanID, ct)
                    ?? throw new BusinessRuleException("La planificación indicada no existe.");

                await EnsureNoOverlapAsync(
                    entity.EmployeeID, plan.StartDate, plan.EndDate, plan.StartTime, plan.EndTime, null, ct);

                created = await base.CreateAsync(entity, ct);
                await transaction.CommitAsync(ct);
            });

            return created;
        }

        public async Task AcquireOverlapLockAsync(CancellationToken ct = default)
        {
            if (!await _repository.TryAcquireOverlapLockAsync(ct))
                throw new BusinessRuleException(
                    "El sistema está validando otra planificación en este momento. Intente nuevamente en unos segundos.");
        }

        public async Task EnsureNoOverlapAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            int? excludePlanId = null,
            CancellationToken ct = default)
        {
            var overlaps = await _repository.FindOverlappingPlansAsync(
                employeeId, startDate, endDate, startTime, endTime, excludePlanId, ct);

            if (overlaps.Count == 0)
                return;

            var name = await _repository.GetEmployeeDisplayNameAsync(employeeId, ct) ?? $"con ID {employeeId}";
            var detail = string.Join("; ", overlaps.Take(3).Select(DescribePlan));
            var more = overlaps.Count > 3 ? $" y {overlaps.Count - 3} más" : string.Empty;

            throw new BusinessRuleException(
                $"El empleado {name} ya está asignado a otra planificación en el mismo día y hora: {detail}{more}. " +
                "Un empleado no puede estar en dos planificaciones (horas extra o recuperación) al mismo tiempo.");
        }

        private static string DescribePlan(TimePlanning plan)
        {
            var type = plan.PlanType == "Recovery" ? "recuperación" : "horas extra";
            var dates = plan.StartDate.Date == plan.EndDate.Date
                ? $"el {plan.StartDate:dd/MM/yyyy}"
                : $"del {plan.StartDate:dd/MM/yyyy} al {plan.EndDate:dd/MM/yyyy}";
            var from = plan.StartTime.ToString(@"hh\:mm");
            var to = plan.EndTime.ToString(@"hh\:mm");
            return $"«{plan.Title}» ({type}) {dates} de {from} a {to}";
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

        public async Task EnsurePlanScheduleHasNoOverlapAsync(
            int planId,
            DateTime startDate,
            DateTime endDate,
            TimeSpan startTime,
            TimeSpan endTime,
            CancellationToken ct = default)
        {
            if (await _repository.IsPlanCancelledAsync(planId, ct))
                return;

            foreach (var employeeId in await _repository.GetActiveEmployeeIdsByPlanAsync(planId, ct))
                await EnsureNoOverlapAsync(employeeId, startDate, endDate, startTime, endTime, planId, ct);
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