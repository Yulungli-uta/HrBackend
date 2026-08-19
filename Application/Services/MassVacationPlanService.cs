using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.MassVacationPlan;
using WsUtaSystem.Application.DTOs.TimeBalances;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Orquesta la planificación masiva de vacaciones: crear/editar el plan, gestionar
/// exclusiones individuales, y las transiciones automáticas por fecha (PLANNED ->
/// IN_PROGRESS -> FINISHED, vía <see cref="ProcessDueTransitionsAsync"/> llamado por
/// DailyMassVacationPlanTransitionJob) que descuentan saldo con
/// <see cref="IVacationBalanceAdjustmentService.BulkAdjustAsync"/> — sin crear filas en
/// HR.tbl_Vacations; el cruce con asistencia y el historial personal leen directamente el
/// plan + sus exclusiones. El estado se resuelve por HR.ref_Types (categoría
/// MASS_VACATION_PLAN_STATUS), no por string hardcodeado — mismo patrón que
/// GuardVacationPlan.StatusTypeId.
/// </summary>
public class MassVacationPlanService : IMassVacationPlanService
{
    private const string StatusCategory = "MASS_VACATION_PLAN_STATUS";
    private const string StatusPlanned = "PLANNED";
    private const string StatusInProgress = "IN_PROGRESS";
    private const string StatusFinished = "FINISHED";
    private const string StatusCancelled = "CANCELLED";

    private readonly IMassVacationPlanRepository _repo;
    private readonly IVacationBalanceAdjustmentService _balanceAdjustment;
    private readonly IParametersRepository _parametersRepository;
    private readonly AppDbContext _db;

    public MassVacationPlanService(
        IMassVacationPlanRepository repo,
        IVacationBalanceAdjustmentService balanceAdjustment,
        IParametersRepository parametersRepository,
        AppDbContext db)
    {
        _repo = repo;
        _balanceAdjustment = balanceAdjustment;
        _parametersRepository = parametersRepository;
        _db = db;
    }

    public async Task<List<MassVacationPlanDto>> GetAllAsync(CancellationToken ct)
    {
        var statusTypes = await GetStatusTypesAsync(ct);
        var plans = await _db.Set<MassVacationPlan>().AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var result = new List<MassVacationPlanDto>(plans.Count);
        foreach (var p in plans)
            result.Add(await MapToDtoAsync(p, statusTypes, ct));
        return result;
    }

    public async Task<PagedResult<MassVacationPlanDto>> GetPagedAsync(
        int page, int pageSize, string? search, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.Set<MassVacationPlan>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Description != null && p.Description.ToLower().Contains(term));
        }

        // Solapamiento de rango: el plan cruza con [fromDate, toDate] si su inicio no es
        // posterior al "hasta" pedido y su fin no es anterior al "desde" pedido.
        if (fromDate.HasValue)
            query = query.Where(p => p.EndDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.StartDate <= toDate.Value);

        var totalCount = await query.LongCountAsync(ct);

        var pageItems = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var statusTypes = await GetStatusTypesAsync(ct);
        var dtoItems = new List<MassVacationPlanDto>(pageItems.Count);
        foreach (var p in pageItems)
            dtoItems.Add(await MapToDtoAsync(p, statusTypes, ct));

        return new PagedResult<MassVacationPlanDto>
        {
            Items = dtoItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<MassVacationPlanDto?> GetByIdAsync(int planId, CancellationToken ct)
    {
        var plan = await _db.Set<MassVacationPlan>().AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        if (plan is null) return null;
        var statusTypes = await GetStatusTypesAsync(ct);
        return await MapToDtoAsync(plan, statusTypes, ct);
    }

    public async Task<MassVacationPlanDto> CreateAsync(MassVacationPlanCreateDto dto, int? createdByEmployeeId, CancellationToken ct)
    {
        ValidateDates(dto.StartDate, dto.EndDate, dto.StartTime, dto.EndTime);

        var statusTypes = await GetStatusTypesAsync(ct);
        var plannedTypeId = GetStatusTypeId(statusTypes, StatusPlanned);

        var entity = new MassVacationPlan
        {
            DepartmentId = dto.DepartmentId,
            Description = dto.Description?.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            VacationYear = dto.VacationYear,
            StatusTypeId = plannedTypeId,
            CreatedBy = createdByEmployeeId,
            CreatedAt = DateTime.UtcNow,
        };

        await _repo.AddAsync(entity, ct);

        return await MapToDtoAsync(entity, statusTypes, ct);
    }

    public async Task<MassVacationPlanDto> UpdateAsync(int planId, MassVacationPlanUpdateDto dto, int? performedByEmployeeId, CancellationToken ct)
    {
        var plan = await _db.Set<MassVacationPlan>().FirstOrDefaultAsync(p => p.PlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones masivas {planId} no existe.");

        var statusTypes = await GetStatusTypesAsync(ct);
        var plannedTypeId = GetStatusTypeId(statusTypes, StatusPlanned);

        if (plan.StatusTypeId != plannedTypeId)
        {
            var currentLabel = statusTypes.First(r => r.TypeId == plan.StatusTypeId).Description;
            throw new BusinessRuleException($"Solo se puede editar un plan en estado 'Planificado'. Estado actual: '{currentLabel}'.");
        }

        ValidateDates(dto.StartDate, dto.EndDate, dto.StartTime, dto.EndTime);

        plan.DepartmentId = dto.DepartmentId;
        plan.Description = dto.Description?.Trim();
        plan.StartDate = dto.StartDate;
        plan.EndDate = dto.EndDate;
        plan.StartTime = dto.StartTime;
        plan.EndTime = dto.EndTime;
        plan.VacationYear = dto.VacationYear;
        plan.UpdatedBy = performedByEmployeeId;
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await MapToDtoAsync(plan, statusTypes, ct);
    }

    private static void ValidateDates(DateOnly startDate, DateOnly endDate, TimeOnly? startTime, TimeOnly? endTime)
    {
        if (endDate < startDate)
            throw new BusinessRuleException("La fecha de fin no puede ser anterior a la fecha de inicio.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (startDate <= today)
            throw new BusinessRuleException("La fecha de inicio debe ser estrictamente futura (no hoy, no una fecha pasada).");

        var hasTimeRange = startTime.HasValue || endTime.HasValue;
        if (hasTimeRange)
        {
            if (startTime is null || endTime is null)
                throw new BusinessRuleException("El modo por horas requiere hora de inicio y hora de fin.");
            if (startDate != endDate)
                throw new BusinessRuleException("El modo por horas solo aplica a un único día (fecha de inicio debe ser igual a la fecha de fin).");
            if (endTime <= startTime)
                throw new BusinessRuleException("La hora de fin debe ser posterior a la hora de inicio.");
        }
    }

    public Task<List<MassVacationPlanRosterItemDto>> GetRosterAsync(int planId, CancellationToken ct) =>
        _repo.GetRosterAsync(planId, ct);

    public async Task SetExclusionAsync(int planId, MassVacationPlanExclusionSetDto dto, int? performedByEmployeeId, CancellationToken ct)
    {
        var plan = await _db.Set<MassVacationPlan>().AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones masivas {planId} no existe.");

        var statusTypes = await GetStatusTypesAsync(ct);
        var plannedTypeId = GetStatusTypeId(statusTypes, StatusPlanned);

        if (plan.StatusTypeId != plannedTypeId)
        {
            var currentLabel = statusTypes.First(r => r.TypeId == plan.StatusTypeId).Description;
            throw new BusinessRuleException($"El plan está en estado '{currentLabel}'; las exclusiones solo se pueden editar mientras está en 'Planificado'.");
        }

        var existing = await _repo.GetExclusionAsync(planId, dto.EmployeeId, ct);

        if (dto.IsExcluded)
        {
            if (existing != null) return; // ya estaba excluido, idempotente
            await _repo.AddExclusionAsync(new MassVacationPlanExclusion
            {
                PlanId = planId,
                EmployeeId = dto.EmployeeId,
                Reason = dto.Reason,
                CreatedBy = performedByEmployeeId,
                CreatedAt = DateTime.UtcNow,
            }, ct);
        }
        else
        {
            if (existing is null) return; // ya no estaba excluido, idempotente
            await _repo.RemoveExclusionAsync(existing, ct);
        }
    }

    public async Task CancelAsync(int planId, string? reason, int? performedByEmployeeId, CancellationToken ct)
    {
        var plan = await _db.Set<MassVacationPlan>().FirstOrDefaultAsync(p => p.PlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones masivas {planId} no existe.");

        var statusTypes = await GetStatusTypesAsync(ct);
        var plannedTypeId = GetStatusTypeId(statusTypes, StatusPlanned);
        var cancelledTypeId = GetStatusTypeId(statusTypes, StatusCancelled);

        if (plan.StatusTypeId != plannedTypeId)
        {
            var currentLabel = statusTypes.First(r => r.TypeId == plan.StatusTypeId).Description;
            throw new BusinessRuleException($"Solo se puede anular un plan en estado 'Planificado'. Estado actual: '{currentLabel}'.");
        }

        plan.StatusTypeId = cancelledTypeId;
        plan.UpdatedBy = performedByEmployeeId;
        plan.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(reason))
            plan.Description = string.IsNullOrWhiteSpace(plan.Description)
                ? $"[Anulado: {reason.Trim()}]"
                : $"{plan.Description} [Anulado: {reason.Trim()}]";

        await _db.SaveChangesAsync(ct);
    }

    public async Task<MassVacationPlanTransitionRunResultDto> ProcessDueTransitionsAsync(int? performedByEmployeeId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var result = new MassVacationPlanTransitionRunResultDto();

        var statusTypes = await GetStatusTypesAsync(ct);
        var plannedTypeId = GetStatusTypeId(statusTypes, StatusPlanned);
        var inProgressTypeId = GetStatusTypeId(statusTypes, StatusInProgress);
        var finishedTypeId = GetStatusTypeId(statusTypes, StatusFinished);

        // 1) PLANNED -> IN_PROGRESS (con descuento de saldo), uno por uno — si uno falla no
        // debe bloquear a los demás planes de la misma corrida.
        var duePlanIds = await _db.Set<MassVacationPlan>().AsNoTracking()
            .Where(p => p.StatusTypeId == plannedTypeId && !p.IsDeleted && p.StartDate <= today)
            .Select(p => p.PlanId)
            .ToListAsync(ct);

        foreach (var planId in duePlanIds)
        {
            var executeResult = await StartExecutionAsync(planId, plannedTypeId, inProgressTypeId, performedByEmployeeId, ct);
            result.StartedPlans.Add(executeResult);
        }

        // 2) IN_PROGRESS -> FINISHED (sin tocar saldo, ya se descontó en el paso 1).
        var toFinish = await _db.Set<MassVacationPlan>()
            .Where(p => p.StatusTypeId == inProgressTypeId && !p.IsDeleted && p.EndDate < today)
            .ToListAsync(ct);

        foreach (var plan in toFinish)
        {
            plan.StatusTypeId = finishedTypeId;
            plan.UpdatedAt = DateTime.UtcNow;
            plan.UpdatedBy = performedByEmployeeId;
            result.FinishedPlanIds.Add(plan.PlanId);
        }

        if (toFinish.Count > 0)
            await _db.SaveChangesAsync(ct);

        return result;
    }

    /// <summary>Descuenta el saldo de los incluidos y pasa el plan a IN_PROGRESS. Extraído
    /// aparte para que tanto el job diario como una futura ejecución manual de soporte lo
    /// puedan invocar igual.</summary>
    private async Task<MassVacationPlanExecuteResultDto> StartExecutionAsync(
        int planId, int plannedTypeId, int inProgressTypeId, int? performedByEmployeeId, CancellationToken ct)
    {
        var plan = await _db.Set<MassVacationPlan>().FirstOrDefaultAsync(p => p.PlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones masivas {planId} no existe.");

        if (plan.StatusTypeId != plannedTypeId)
            throw new BusinessRuleException($"El plan {planId} ya no está en 'Planificado'.");

        var includedIds = await _repo.GetIncludedEmployeeIdsAsync(planId, ct);

        int minutesToDeduct;
        if (plan.StartTime.HasValue && plan.EndTime.HasValue)
        {
            // Modo "por horas": minutos reales de esa franja, no un día completo.
            minutesToDeduct = (int)(plan.EndTime.Value.ToTimeSpan() - plan.StartTime.Value.ToTimeSpan()).TotalMinutes;
        }
        else
        {
            var calendarDays = plan.EndDate.DayNumber - plan.StartDate.DayNumber + 1;
            var workMinutesPerDay = await GetWorkMinutesPerDayAsync(ct);
            minutesToDeduct = calendarDays * workMinutesPerDay;
        }

        var employeesInfo = await (
            from e in _db.Employees.AsNoTracking()
            where includedIds.Contains(e.EmployeeId)
            join person in _db.People.AsNoTracking() on e.PersonID equals person.PersonId
            select new { e.EmployeeId, person.IdCard }
        ).ToListAsync(ct);

        var regimesByEmployee = await _db.Set<EmployeeLaborRegime>().AsNoTracking()
            .Where(r => includedIds.Contains(r.EmployeeId) && r.IsActive)
            .ToListAsync(ct);

        var regimeIds = regimesByEmployee.Select(r => r.LaborRegimeId).Distinct().ToList();
        var regimeNames = await _db.Set<RefTypes>().AsNoTracking()
            .Where(r => regimeIds.Contains(r.TypeId))
            .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        var items = new List<VacationBalanceBulkAdjustmentItemDto>();
        var skipped = new List<MassVacationPlanExecuteRowResultDto>();
        var idCardByEmployee = employeesInfo.ToDictionary(x => x.EmployeeId, x => x.IdCard);

        var periodLabel = plan.StartTime.HasValue && plan.EndTime.HasValue
            ? $"{plan.StartDate:yyyy-MM-dd} {plan.StartTime:hh\\:mm} a {plan.EndTime:hh\\:mm}"
            : $"{plan.StartDate:yyyy-MM-dd} a {plan.EndDate:yyyy-MM-dd}";

        foreach (var employeeId in includedIds)
        {
            var idCard = idCardByEmployee.TryGetValue(employeeId, out var ic) ? ic : null;
            var principal = regimesByEmployee
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.IsPrincipal)
                .FirstOrDefault();

            if (idCard is null || principal is null || !regimeNames.TryGetValue(principal.LaborRegimeId, out var regimeName))
            {
                skipped.Add(new MassVacationPlanExecuteRowResultDto
                {
                    EmployeeId = employeeId,
                    IdCard = idCard ?? "",
                    Success = false,
                    Message = "No se pudo resolver cédula o régimen laboral activo.",
                });
                continue;
            }

            items.Add(new VacationBalanceBulkAdjustmentItemDto
            {
                Cedula = idCard,
                LaborRegimeName = regimeName,
                BalanceField = TimeBalanceField.Vacation,
                Mode = VacationBalanceAdjustmentMode.Increment,
                ValueMinutes = -minutesToDeduct,
                Reason = $"Vacación institucional — Plan #{planId} ({periodLabel})",
                AllowNegativeResult = true,
            });
        }

        var bulkResults = items.Count > 0
            ? await _balanceAdjustment.BulkAdjustAsync(
                new VacationBalanceBulkAdjustmentRequestDto { BatchTag = $"MASSVAC_{planId}", Items = items },
                performedByEmployeeId, ct)
            : [];

        var rows = bulkResults
            .Select(r => new MassVacationPlanExecuteRowResultDto
            {
                EmployeeId = employeesInfo.FirstOrDefault(x => x.IdCard == r.Cedula)?.EmployeeId ?? 0,
                IdCard = r.Cedula,
                Success = r.Success,
                Message = r.Message,
            })
            .Concat(skipped)
            .ToList();

        plan.StatusTypeId = inProgressTypeId;
        plan.ExecutedBy = performedByEmployeeId;
        plan.ExecutedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new MassVacationPlanExecuteResultDto
        {
            PlanId = planId,
            TotalProcessed = includedIds.Count,
            TotalSuccess = rows.Count(r => r.Success),
            TotalFailed = rows.Count(r => !r.Success),
            Rows = rows,
        };
    }

    public async Task<List<MassVacationPlanDto>> GetApplicablePlansForEmployeeAsync(int employeeId, CancellationToken ct)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);
        if (employee is null) return [];

        var statusTypes = await GetStatusTypesAsync(ct);
        var inProgressTypeId = GetStatusTypeId(statusTypes, StatusInProgress);
        var finishedTypeId = GetStatusTypeId(statusTypes, StatusFinished);

        var excludedPlanIds = await _db.Set<MassVacationPlanExclusion>().AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .Select(x => x.PlanId)
            .ToListAsync(ct);

        var plans = await _db.Set<MassVacationPlan>().AsNoTracking()
            .Where(p => (p.StatusTypeId == inProgressTypeId || p.StatusTypeId == finishedTypeId)
                && !p.IsDeleted
                && (p.DepartmentId == null || p.DepartmentId == employee.DepartmentId)
                && !excludedPlanIds.Contains(p.PlanId))
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);

        var result = new List<MassVacationPlanDto>(plans.Count);
        foreach (var p in plans)
            result.Add(await MapToDtoAsync(p, statusTypes, ct));
        return result;
    }

    private async Task<List<RefTypes>> GetStatusTypesAsync(CancellationToken ct) =>
        await _db.Set<RefTypes>().AsNoTracking()
            .Where(r => r.Category == StatusCategory)
            .ToListAsync(ct);

    private static int GetStatusTypeId(List<RefTypes> statusTypes, string name)
    {
        var match = statusTypes.FirstOrDefault(r => r.Name == name);
        if (match is null)
            throw new InvalidOperationException($"RefType no encontrado: {StatusCategory}/{name}. Verifique el catálogo HR.ref_Types.");
        return match.TypeId;
    }

    private async Task<MassVacationPlanDto> MapToDtoAsync(MassVacationPlan plan, List<RefTypes> statusTypes, CancellationToken ct)
    {
        string? deptName = null;
        if (plan.DepartmentId.HasValue)
        {
            deptName = await _db.Departments.AsNoTracking()
                .Where(d => d.DepartmentId == plan.DepartmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(ct);
        }

        var excludedCount = await _db.Set<MassVacationPlanExclusion>().AsNoTracking()
            .CountAsync(x => x.PlanId == plan.PlanId, ct);

        var scopeQuery = _db.Employees.AsNoTracking().Where(e => e.IsActive);
        if (plan.DepartmentId.HasValue)
            scopeQuery = scopeQuery.Where(e => e.DepartmentId == plan.DepartmentId.Value);
        var scopeCount = await scopeQuery.CountAsync(ct);

        var statusType = statusTypes.FirstOrDefault(r => r.TypeId == plan.StatusTypeId);

        return new MassVacationPlanDto
        {
            PlanId = plan.PlanId,
            DepartmentId = plan.DepartmentId,
            DepartmentName = deptName,
            Description = plan.Description,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            StartTime = plan.StartTime,
            EndTime = plan.EndTime,
            VacationYear = plan.VacationYear,
            StatusTypeId = plan.StatusTypeId,
            Status = statusType?.Name ?? string.Empty,
            StatusLabel = statusType?.Description ?? statusType?.Name ?? string.Empty,
            TotalEmployeesInScope = scopeCount,
            TotalExcluded = excludedCount,
            ExecutedBy = plan.ExecutedBy,
            ExecutedAt = plan.ExecutedAt,
            CreatedAt = plan.CreatedAt,
        };
    }

    private async Task<int> GetWorkMinutesPerDayAsync(CancellationToken ct)
    {
        var list = await _parametersRepository.GetByNameAsync("WORK_MINUTES_PER_DAY", ct);
        var value = list?.FirstOrDefault(p => p.IsActive)?.Pvalues;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 480;
    }
}
