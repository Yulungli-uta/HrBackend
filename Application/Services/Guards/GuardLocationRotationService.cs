using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardLocationRotationService : IGuardLocationRotationService
{
    private readonly IGuardLocationRotationPeriodRepository _periodRepo;
    private readonly IGuardLocationRotationAssignmentRepository _assignmentRepo;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GuardLocationRotationService(
        IGuardLocationRotationPeriodRepository periodRepo,
        IGuardLocationRotationAssignmentRepository assignmentRepo,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _periodRepo = periodRepo;
        _assignmentRepo = assignmentRepo;
        _db = db;
        _currentUser = currentUser;
    }

    // ─── Periodos ─────────────────────────────────────────────────────────────

    public async Task<List<GuardLocationRotationPeriodDto>> GetPeriodsAsync(CancellationToken ct) =>
        await _db.GuardLocationRotationPeriods
            .OrderByDescending(p => p.StartDate)
            .Select(p => new GuardLocationRotationPeriodDto(
                p.LocationRotationPeriodId,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.Notes,
                p.Assignments.Count(a => a.IsActive)))
            .ToListAsync(ct);

    public async Task<PagedResult<GuardLocationRotationPeriodDto>> GetPeriodsPagedAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _db.GuardLocationRotationPeriods.LongCountAsync(ct);
        var items = await _db.GuardLocationRotationPeriods
            .OrderByDescending(p => p.StartDate)
            .Select(p => new GuardLocationRotationPeriodDto(
                p.LocationRotationPeriodId,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.Notes,
                p.Assignments.Count(a => a.IsActive)))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<GuardLocationRotationPeriodDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardLocationRotationPeriodDto?> GetPeriodByIdAsync(int periodId, CancellationToken ct) =>
        await _db.GuardLocationRotationPeriods
            .Where(p => p.LocationRotationPeriodId == periodId)
            .Select(p => new GuardLocationRotationPeriodDto(
                p.LocationRotationPeriodId,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.Notes,
                p.Assignments.Count(a => a.IsActive)))
            .FirstOrDefaultAsync(ct);

    public async Task<GuardLocationRotationPeriodDto> CreatePeriodAsync(CreateGuardLocationRotationPeriodDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear periodos de rotación.");

        var entity = new GuardLocationRotationPeriod
        {
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Notes = dto.Notes,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _periodRepo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetPeriodByIdAsync(entity.LocationRotationPeriodId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el periodo creado.");
    }

    public async Task<GuardLocationRotationPeriodDto> UpdatePeriodAsync(int periodId, UpdateGuardLocationRotationPeriodDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardLocationRotationPeriods
            .FirstOrDefaultAsync(p => p.LocationRotationPeriodId == periodId, ct)
            ?? throw new KeyNotFoundException($"Periodo de rotación {periodId} no encontrado.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar periodos.");

        entity.Name = dto.Name;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.Notes = dto.Notes;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetPeriodByIdAsync(periodId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el periodo actualizado.");
    }

    // ─── Asignaciones ─────────────────────────────────────────────────────────

    public async Task<List<GuardLocationRotationAssignmentDto>> GetAssignmentsByPeriodAsync(int periodId, CancellationToken ct)
    {
        var items = await _assignmentRepo.GetByPeriodAsync(periodId, ct);
        return items.Select(MapAssignmentToDto).ToList();
    }

    public async Task<List<GuardLocationRotationAssignmentDto>> GetAssignmentsByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        var items = await _db.GuardLocationRotationAssignments
            .Include(a => a.Period)
            .Include(a => a.Group)
            .Include(a => a.Employee).ThenInclude(e => e!.People)
            .Include(a => a.Location)
            .Include(a => a.PriorityType)
            .Where(a => a.EmployeeId == employeeId && a.IsActive && a.Period!.IsActive)
            .OrderByDescending(a => a.Period!.StartDate)
            .ToListAsync(ct);
        return items.Select(MapAssignmentToDto).ToList();
    }

    public async Task<GuardLocationRotationAssignmentDto> CreateAssignmentAsync(CreateGuardLocationRotationAssignmentDto dto, CancellationToken ct)
    {
        if (dto.GroupId is null && dto.EmployeeId is null)
            throw new ArgumentException("Se debe especificar un grupo o un empleado para la asignación.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear asignaciones.");

        var entity = new GuardLocationRotationAssignment
        {
            LocationRotationPeriodId = dto.LocationRotationPeriodId,
            GroupId = dto.GroupId,
            EmployeeId = dto.EmployeeId,
            LocationId = dto.LocationId,
            PriorityTypeId = dto.PriorityTypeId,
            IsFixedLocation = dto.IsFixedLocation,
            IsFixedSchedule = dto.IsFixedSchedule,
            Notes = dto.Notes,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _assignmentRepo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        var loaded = await _db.GuardLocationRotationAssignments
            .Include(a => a.Period)
            .Include(a => a.Group)
            .Include(a => a.Employee).ThenInclude(e => e!.People)
            .Include(a => a.Location)
            .Include(a => a.PriorityType)
            .FirstOrDefaultAsync(a => a.LocationRotationAssignmentId == entity.LocationRotationAssignmentId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la asignación creada.");

        return MapAssignmentToDto(loaded);
    }

    public async Task<GuardLocationRotationAssignmentDto> UpdateAssignmentAsync(int assignmentId, UpdateGuardLocationRotationAssignmentDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardLocationRotationAssignments
            .FirstOrDefaultAsync(a => a.LocationRotationAssignmentId == assignmentId, ct)
            ?? throw new KeyNotFoundException($"Asignación de rotación {assignmentId} no encontrada.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar asignaciones.");

        entity.LocationId = dto.LocationId;
        entity.PriorityTypeId = dto.PriorityTypeId;
        entity.IsFixedLocation = dto.IsFixedLocation;
        entity.IsFixedSchedule = dto.IsFixedSchedule;
        entity.Notes = dto.Notes;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var loaded = await _db.GuardLocationRotationAssignments
            .Include(a => a.Period)
            .Include(a => a.Group)
            .Include(a => a.Employee).ThenInclude(e => e!.People)
            .Include(a => a.Location)
            .Include(a => a.PriorityType)
            .FirstOrDefaultAsync(a => a.LocationRotationAssignmentId == assignmentId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la asignación actualizada.");

        return MapAssignmentToDto(loaded);
    }

    public async Task DeleteAssignmentAsync(int assignmentId, CancellationToken ct)
    {
        var entity = await _db.GuardLocationRotationAssignments
            .FirstOrDefaultAsync(a => a.LocationRotationAssignmentId == assignmentId, ct)
            ?? throw new KeyNotFoundException($"Asignación de rotación {assignmentId} no encontrada.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede eliminar asignaciones.");

        entity.IsActive = false;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static GuardLocationRotationAssignmentDto MapAssignmentToDto(GuardLocationRotationAssignment a) =>
        new(
            a.LocationRotationAssignmentId,
            a.LocationRotationPeriodId,
            a.Period?.Name ?? string.Empty,
            a.GroupId,
            a.Group?.Name,
            a.Group?.GroupCode,
            a.EmployeeId,
            a.Employee is null ? null : a.Employee.People.GetFullName(),
            a.Employee?.People?.IdCard,
            a.LocationId,
            a.Location?.LocationName ?? string.Empty,
            a.Location?.LocationCode,
            a.PriorityTypeId,
            a.PriorityType?.Name,
            a.IsFixedLocation,
            a.IsFixedSchedule,
            a.Notes,
            a.IsActive
        );
}
