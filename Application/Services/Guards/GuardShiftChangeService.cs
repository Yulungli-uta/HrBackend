using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardShiftChangeService : IGuardShiftChangeService
{
    private readonly IGuardShiftChangeRepository _repo;
    private readonly IGuardAssignmentValidationService _validationService;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GuardShiftChangeService(
        IGuardShiftChangeRepository repo,
        IGuardAssignmentValidationService validationService,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _validationService = validationService;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<GuardShiftChangeDto>> GetByPlanningAsync(int planningId, CancellationToken ct)
    {
        var changes = await _repo.GetByPlanningIdAsync(planningId, ct);
        return changes.Select(MapToDto).ToList();
    }

    public async Task<List<GuardShiftChangeDto>> GetPendingAsync(CancellationToken ct)
    {
        var changes = await _repo.GetPendingChangesAsync(ct);
        return changes.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<GuardShiftChangeDto>> GetPendingPagedAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.GuardShiftChanges
            .Include(c => c.Planning)
            .Include(c => c.OriginalEmployee).ThenInclude(e => e!.People)
            .Include(c => c.ReplacementEmployee).ThenInclude(e => e!.People)
            .Include(c => c.OriginalSchedule)
            .Include(c => c.NewSchedule)
            .Include(c => c.ChangeType)
            .Include(c => c.StatusType)
            .Where(c => c.StatusType!.Name == "PENDING")
            .OrderByDescending(c => c.RequestedAt);

        var total = await q.LongCountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<GuardShiftChangeDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardShiftChangeDto> CreateReplacementAsync(CreateGuardShiftReplacementDto dto, CancellationToken ct)
    {
        var planning = await _db.GuardShiftPlannings
            .Include(p => p.Schedule)
            .FirstOrDefaultAsync(p => p.PlanningId == dto.PlanningId, ct)
            ?? throw new KeyNotFoundException($"Planificación {dto.PlanningId} no encontrada.");

        if (planning.EmployeeId == dto.ReplacementEmployeeId)
            throw new InvalidOperationException("El reemplazante no puede ser el mismo empleado titular.");

        var validateReq = new ValidateGuardAssignmentRequestDto(
            dto.ReplacementEmployeeId, planning.LocationId, planning.WorkDate,
            dto.NewScheduleId ?? planning.ScheduleId, planning.PlanningId, false);
        var validation = await _validationService.ValidateAsync(validateReq, ct);

        if (validation.HasBlockingErrors)
            throw new InvalidOperationException(
                "El reemplazante no está disponible: " +
                string.Join("; ", validation.Validations.Where(v => v.Severity == "BLOCKING").Select(v => v.Message)));

        var pendingTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_CHANGE_STATUS" && r.Name == "PENDING")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var entity = new GuardShiftChange
        {
            PlanningId = dto.PlanningId,
            OriginalEmployeeId = planning.EmployeeId,
            ReplacementEmployeeId = dto.ReplacementEmployeeId,
            OriginalScheduleId = planning.ScheduleId,
            NewScheduleId = dto.NewScheduleId,
            ChangeTypeId = dto.ChangeTypeId,
            StatusTypeId = pendingTypeId,
            IsActiveForAttendance = false,
            Reason = dto.Reason,
            RequestedBy = _currentUser.EmployeeId,
            RequestedAt = DateTime.UtcNow
        };

        await _db.GuardShiftChanges.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<GuardShiftChangeDto> ApproveAsync(int shiftChangeId, ApproveGuardShiftChangeDto dto, CancellationToken ct)
    {
        var change = await _db.GuardShiftChanges
            .Include(c => c.StatusType)
            .FirstOrDefaultAsync(c => c.ShiftChangeId == shiftChangeId, ct)
            ?? throw new KeyNotFoundException($"Cambio {shiftChangeId} no encontrado.");

        if (change.StatusType?.Name != "PENDING")
            throw new InvalidOperationException("Solo se pueden aprobar cambios en estado Pendiente.");

        var approvedTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_CHANGE_STATUS" && r.Name == "APPROVED")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var replacedStatusTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_PLANNING_STATUS" && r.Name == "REPLACED")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var previousActive = await _db.GuardShiftChanges
            .Where(c => c.PlanningId == change.PlanningId && c.IsActiveForAttendance && c.ShiftChangeId != shiftChangeId)
            .ToListAsync(ct);
        foreach (var prev in previousActive)
            prev.IsActiveForAttendance = false;

        change.StatusTypeId = approvedTypeId;
        change.IsActiveForAttendance = true;
        change.ApprovedBy = _currentUser.EmployeeId;
        change.ApprovedAt = DateTime.UtcNow;

        var planning = await _db.GuardShiftPlannings.FindAsync(new object[] { change.PlanningId }, ct);
        if (planning is not null)
            planning.StatusTypeId = replacedStatusTypeId;

        await _db.SaveChangesAsync(ct);
        return MapToDto(change);
    }

    public async Task<GuardShiftChangeDto> RejectAsync(int shiftChangeId, RejectGuardShiftChangeDto dto, CancellationToken ct)
    {
        var change = await _db.GuardShiftChanges
            .Include(c => c.StatusType)
            .FirstOrDefaultAsync(c => c.ShiftChangeId == shiftChangeId, ct)
            ?? throw new KeyNotFoundException($"Cambio {shiftChangeId} no encontrado.");

        if (change.StatusType?.Name != "PENDING")
            throw new InvalidOperationException("Solo se pueden rechazar cambios en estado Pendiente.");

        var rejectedTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_CHANGE_STATUS" && r.Name == "REJECTED")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        change.StatusTypeId = rejectedTypeId;
        change.RejectionReason = dto.RejectionReason;
        change.ApprovedBy = _currentUser.EmployeeId;
        change.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDto(change);
    }

    private static GuardShiftChangeDto MapToDto(GuardShiftChange c) =>
        new(c.ShiftChangeId, c.PlanningId,
            c.Planning?.WorkDate ?? default,
            c.OriginalEmployeeId,
            c.OriginalEmployee is null ? "" : $"{c.OriginalEmployee.People?.FirstName} {c.OriginalEmployee.People?.LastName}",
            c.ReplacementEmployeeId,
            c.ReplacementEmployee is null ? null : $"{c.ReplacementEmployee.People?.FirstName} {c.ReplacementEmployee.People?.LastName}",
            c.OriginalScheduleId, c.OriginalSchedule?.Description ?? "",
            c.NewScheduleId, c.NewSchedule?.Description,
            c.ChangeType?.Name ?? "", c.StatusType?.Name ?? "",
            c.IsActiveForAttendance, c.Reason, c.RequestedAt, c.RequestedBy, null,
            c.ApprovedBy, null, c.ApprovedAt, c.RejectionReason);
}
