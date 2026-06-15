using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardAssignmentValidationService : IGuardAssignmentValidationService
{
    private readonly IGuardAssignmentValidationRepository _repo;
    private readonly IEmployeeAvailabilityBlockRepository _blockRepo;
    private readonly IGuardShiftPlanningRepository _planningRepo;
    private readonly AppDbContext _db;

    public GuardAssignmentValidationService(
        IGuardAssignmentValidationRepository repo,
        IEmployeeAvailabilityBlockRepository blockRepo,
        IGuardShiftPlanningRepository planningRepo,
        AppDbContext db)
    {
        _repo = repo;
        _blockRepo = blockRepo;
        _planningRepo = planningRepo;
        _db = db;
    }

    public async Task<List<GuardAssignmentValidationDto>> GetByPlanningAsync(int planningId, CancellationToken ct)
    {
        var validations = await _repo.GetByPlanningIdAsync(planningId, ct);
        return validations.Select(MapToDto).ToList();
    }

    public async Task<List<GuardAssignmentValidationDto>> GetByEmployeeAsync(int employeeId, int limit, CancellationToken ct)
    {
        var validations = await _repo.GetByEmployeeIdAsync(employeeId, limit, ct);
        return validations.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<GuardAssignmentValidationDto>> GetByPlanningPagedAsync(
        int planningId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Set<GuardAssignmentValidation>()
            .Include(v => v.Employee).ThenInclude(e => e!.People)
            .Include(v => v.ValidationType)
            .Include(v => v.ResultType)
            .Include(v => v.SeverityType)
            .Where(v => v.PlanningId == planningId)
            .OrderByDescending(v => v.ValidationDate);

        var total = await q.LongCountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<GuardAssignmentValidationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ValidateGuardAssignmentResultDto> ValidateAsync(ValidateGuardAssignmentRequestDto dto, CancellationToken ct)
    {
        var results = new List<(string type, string result, string severity, string message)>();

        var typeIds = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_VALIDATION_TYPE" ||
                        r.Category == "GUARD_VALIDATION_RESULT" ||
                        r.Category == "GUARD_VALIDATION_SEVERITY")
            .ToDictionaryAsync(r => $"{r.Category}:{r.Name}", r => r.TypeId, ct);

        // 1. Empleado activo
        var employee = await _db.Set<WsUtaSystem.Models.Employees>()
            .FirstOrDefaultAsync(e => e.EmployeeId == dto.EmployeeId, ct);

        if (employee is null || !employee.IsActive)
        {
            results.Add(("INACTIVE_EMPLOYEE", "FAILED", "BLOCKING", "El empleado está inactivo o no existe."));
            return BuildResult(results, dto, typeIds, null);
        }

        // 2. Doble turno
        var hasDoubleShift = await _planningRepo.HasActiveShiftOnDateAsync(dto.EmployeeId, dto.WorkDate, dto.PlanningId, ct);
        if (hasDoubleShift)
        {
            if (!dto.AllowDoubleShiftOverride)
                results.Add(("DOUBLE_SHIFT", "FAILED", "BLOCKING", $"El empleado ya tiene un turno asignado el {dto.WorkDate:dd/MM/yyyy}."));
            else
                results.Add(("DOUBLE_SHIFT", "OVERRIDDEN", "WARNING", $"Doble turno exceptuado para el {dto.WorkDate:dd/MM/yyyy}."));
        }

        // 3. Verificar bloqueos (permisos, vacaciones, manual)
        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.ScheduleId == dto.ScheduleId, ct);
        if (schedule is not null)
        {
            var shiftStart = dto.WorkDate.ToDateTime(schedule.EntryTime);
            var shiftEnd = schedule.CrossesMidnight
                ? dto.WorkDate.AddDays(1).ToDateTime(schedule.ExitTime)
                : dto.WorkDate.ToDateTime(schedule.ExitTime);

            var blocks = await _blockRepo.GetActiveBlocksAsync(dto.EmployeeId, shiftStart, shiftEnd, ct);

            foreach (var block in blocks)
            {
                var sourceType = block.SourceType?.Name ?? "MANUAL_BLOCK";
                var (valType, msg) = sourceType switch
                {
                    "PERMISSION" => ("PERMISSION_CONFLICT", "El empleado tiene un permiso aprobado que cruza el turno."),
                    "VACATION"   => ("VACATION_CONFLICT", "El empleado tiene vacaciones aprobadas que cruzan el turno."),
                    _            => ("PERMISSION_CONFLICT", $"El empleado tiene un bloqueo de disponibilidad activo: {block.Reason}.")
                };
                results.Add((valType, "FAILED", "BLOCKING", msg));
            }
        }

        // 4. Verificar descanso mínimo entre turnos consecutivos
        if (schedule is not null)
        {
            var restSettings = await _db.Set<GuardSetting>()
                .Where(s => s.SettingKey == "MINIMUM_REST_HOURS" || s.SettingKey == "MINIMUM_REST_SEVERITY")
                .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, ct);

            if (restSettings.TryGetValue("MINIMUM_REST_HOURS", out var minRestStr) &&
                double.TryParse(minRestStr, out var minRestHours))
            {
                var currentStart = dto.WorkDate.ToDateTime(schedule.EntryTime);

                var previousShift = await _db.Set<GuardShiftPlanning>()
                    .Where(p => p.EmployeeId == dto.EmployeeId
                        && p.WorkDate < dto.WorkDate
                        && p.IsActiveForAssignment)
                    .Include(p => p.Schedule)
                    .OrderByDescending(p => p.WorkDate)
                    .FirstOrDefaultAsync(ct);

                if (previousShift?.Schedule is not null)
                {
                    var prevEnd = previousShift.Schedule.CrossesMidnight
                        ? previousShift.WorkDate.AddDays(1).ToDateTime(previousShift.Schedule.ExitTime)
                        : previousShift.WorkDate.ToDateTime(previousShift.Schedule.ExitTime);

                    var restHours = (currentStart - prevEnd).TotalHours;
                    if (restHours >= 0 && restHours < minRestHours)
                    {
                        var severity = restSettings.TryGetValue("MINIMUM_REST_SEVERITY", out var sev) ? sev : "WARNING";
                        results.Add(("REST_HOURS", "FAILED", severity,
                            $"Descanso insuficiente: {restHours:F1}h previas (mínimo requerido: {minRestHours}h)."));
                    }
                }
            }
        }

        return BuildResult(results, dto, typeIds, null);
    }

    private static ValidateGuardAssignmentResultDto BuildResult(
        List<(string type, string result, string severity, string message)> results,
        ValidateGuardAssignmentRequestDto dto,
        Dictionary<string, int> typeIds,
        int? planningId)
    {
        var dtos = results.Select(r => new GuardAssignmentValidationDto(
            0, dto.EmployeeId, "", planningId, null,
            r.type, r.result, r.severity,
            DateTime.UtcNow, r.message, null
        )).ToList();

        var hasBlocking = dtos.Any(v => v.Severity == "BLOCKING" && v.Result == "FAILED");
        var hasWarnings = dtos.Any(v => v.Severity == "WARNING");

        return new ValidateGuardAssignmentResultDto(!hasBlocking, hasBlocking, hasWarnings, dtos);
    }

    private static GuardAssignmentValidationDto MapToDto(GuardAssignmentValidation v) =>
        new(v.ValidationId, v.EmployeeId,
            v.Employee is null ? "" : $"{v.Employee.People?.FirstName} {v.Employee.People?.LastName}",
            v.PlanningId, v.ShiftChangeId,
            v.ValidationType?.Name ?? "", v.ResultType?.Name ?? "", v.SeverityType?.Name ?? "",
            v.ValidationDate, v.Message, v.Details);
}
