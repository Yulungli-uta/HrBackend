using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardEmployeeSpecialRuleService : IGuardEmployeeSpecialRuleService
{
    private readonly IGuardEmployeeSpecialRuleRepository _repo;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GuardEmployeeSpecialRuleService(
        IGuardEmployeeSpecialRuleRepository repo,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<GuardEmployeeSpecialRuleDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        var items = await _db.GuardEmployeeSpecialRules
            .Where(r => r.EmployeeId == employeeId)
            .Include(r => r.Employee).ThenInclude(e => e!.People)
            .Include(r => r.FixedLocation)
            .Include(r => r.FixedSchedule)
            .OrderByDescending(r => r.ValidFrom)
            .ToListAsync(ct);

        return items.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<GuardEmployeeSpecialRuleDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.GuardEmployeeSpecialRules
            .Include(r => r.Employee).ThenInclude(e => e!.People)
            .Include(r => r.FixedLocation)
            .Include(r => r.FixedSchedule)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(r =>
                (r.Employee!.People!.LastName + " " + r.Employee.People.FirstName).ToLower().Contains(term) ||
                r.Employee.People.IdCard.ToLower().Contains(term));
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(r => r.Employee!.People!.LastName)
            .ThenByDescending(r => r.ValidFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<GuardEmployeeSpecialRuleDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardEmployeeSpecialRuleDto?> GetByIdAsync(int ruleId, CancellationToken ct)
    {
        var item = await _db.GuardEmployeeSpecialRules
            .Include(r => r.Employee).ThenInclude(e => e!.People)
            .Include(r => r.FixedLocation)
            .Include(r => r.FixedSchedule)
            .FirstOrDefaultAsync(r => r.SpecialRuleId == ruleId, ct);

        return item is null ? null : MapToDto(item);
    }

    public async Task<GuardEmployeeSpecialRuleDto> CreateAsync(CreateGuardEmployeeSpecialRuleDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear condiciones especiales.");

        // Validaciones solo al CREAR, no al editar: ya existen reglas activas reales en
        // producción que violan estos 3 chequeos (contradicción Sin-noche/Prioridad-noche,
        // reglas sin ningún parámetro, y solapamiento de fechas entre reglas del mismo
        // empleado) -- bloquearlas también en Update dejaría esas filas sin poder editarse
        // hasta limpiarlas. Esto solo evita que se creen MÁS casos nuevos.
        ValidateNightShiftCoherence(dto.NoNightShift, dto.NightPriority);
        ValidateHasAtLeastOneParameter(dto);
        await EnsureNoOverlappingActiveRuleAsync(dto.EmployeeId, dto.ValidFrom, dto.ValidTo, ct);

        var entity = new GuardEmployeeSpecialRule
        {
            EmployeeId = dto.EmployeeId,
            FixedLocationId = dto.FixedLocationId,
            FixedScheduleId = dto.FixedScheduleId,
            NoNightShift = dto.NoNightShift,
            OnlyWeekDays = dto.OnlyWeekDays,
            WeekendPriority = dto.WeekendPriority,
            NightPriority = dto.NightPriority,
            Reason = dto.Reason,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            RequiresApproval = dto.RequiresApproval,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.SpecialRuleId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la condición especial creada.");
    }

    public async Task<GuardEmployeeSpecialRuleDto> UpdateAsync(int ruleId, UpdateGuardEmployeeSpecialRuleDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardEmployeeSpecialRules
            .FirstOrDefaultAsync(r => r.SpecialRuleId == ruleId, ct)
            ?? throw new KeyNotFoundException($"Condición especial {ruleId} no encontrada.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar condiciones especiales.");

        entity.FixedLocationId = dto.FixedLocationId;
        entity.FixedScheduleId = dto.FixedScheduleId;
        entity.NoNightShift = dto.NoNightShift;
        entity.OnlyWeekDays = dto.OnlyWeekDays;
        entity.WeekendPriority = dto.WeekendPriority;
        entity.NightPriority = dto.NightPriority;
        entity.Reason = dto.Reason;
        entity.ValidFrom = dto.ValidFrom;
        entity.ValidTo = dto.ValidTo;
        entity.RequiresApproval = dto.RequiresApproval;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(ruleId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la condición especial actualizada.");
    }

    private static GuardEmployeeSpecialRuleDto MapToDto(GuardEmployeeSpecialRule r) =>
        new(
            r.SpecialRuleId,
            r.EmployeeId,
            r.Employee is null ? string.Empty : r.Employee.People.GetFullName(),
            r.Employee?.People?.IdCard,
            r.FixedLocationId,
            r.FixedLocation?.LocationName,
            r.FixedLocation?.LocationCode,
            r.FixedScheduleId,
            r.FixedSchedule?.Description,
            r.FixedSchedule?.ScheduleCode,
            r.NoNightShift,
            r.OnlyWeekDays,
            r.WeekendPriority,
            r.NightPriority,
            r.Reason,
            r.ValidFrom,
            r.ValidTo,
            r.RequiresApproval,
            r.IsActive
        );

    private static void ValidateNightShiftCoherence(bool noNightShift, bool nightPriority)
    {
        if (noNightShift && nightPriority)
            throw new InvalidOperationException(
                "No se puede activar 'Sin noche' y 'Prioridad noche' al mismo tiempo — son condiciones contradictorias.");
    }

    private static void ValidateHasAtLeastOneParameter(CreateGuardEmployeeSpecialRuleDto dto)
    {
        var hasAny = dto.FixedLocationId.HasValue || dto.FixedScheduleId.HasValue
            || dto.NoNightShift || dto.OnlyWeekDays || dto.WeekendPriority || dto.NightPriority;

        if (!hasAny)
            throw new InvalidOperationException(
                "La condición especial debe tener al menos un parámetro configurado (ubicación/horario fijo, o alguna de las opciones de restricción).");
    }

    private async Task EnsureNoOverlappingActiveRuleAsync(int employeeId, DateOnly validFrom, DateOnly? validTo, CancellationToken ct)
    {
        var overlaps = await _db.GuardEmployeeSpecialRules.AnyAsync(r =>
            r.EmployeeId == employeeId && r.IsActive
            && r.ValidFrom <= (validTo ?? DateOnly.MaxValue)
            && (r.ValidTo ?? DateOnly.MaxValue) >= validFrom, ct);

        if (overlaps)
            throw new InvalidOperationException(
                "Este empleado ya tiene una condición especial activa vigente en ese rango de fechas.");
    }
}
