using Microsoft.EntityFrameworkCore;
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
                (r.Employee!.People!.FirstName + " " + r.Employee.People.LastName).ToLower().Contains(term) ||
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
            r.Employee is null ? string.Empty : $"{r.Employee.People?.FirstName} {r.Employee.People?.LastName}".Trim(),
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
}
