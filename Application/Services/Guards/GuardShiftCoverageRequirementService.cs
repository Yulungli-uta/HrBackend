using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardShiftCoverageRequirementService : IGuardShiftCoverageRequirementService
{
    private readonly IGuardShiftCoverageRequirementRepository _repo;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GuardShiftCoverageRequirementService(
        IGuardShiftCoverageRequirementRepository repo,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<GuardShiftCoverageRequirementDto>> GetAllAsync(CancellationToken ct)
    {
        var items = await _db.GuardShiftCoverageRequirements
            .Include(r => r.Location)
            .Include(r => r.Schedule)
            .OrderBy(r => r.LocationId)
            .ThenBy(r => r.DayOfWeek)
            .ToListAsync(ct);

        return items.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<GuardShiftCoverageRequirementDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _db.GuardShiftCoverageRequirements.LongCountAsync(ct);
        var items = await _db.GuardShiftCoverageRequirements
            .Include(r => r.Location)
            .Include(r => r.Schedule)
            .OrderBy(r => r.LocationId)
            .ThenBy(r => r.DayOfWeek)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<GuardShiftCoverageRequirementDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardShiftCoverageRequirementDto?> GetByIdAsync(int requirementId, CancellationToken ct)
    {
        var item = await _db.GuardShiftCoverageRequirements
            .Include(r => r.Location)
            .Include(r => r.Schedule)
            .FirstOrDefaultAsync(r => r.RequirementId == requirementId, ct);

        return item is null ? null : MapToDto(item);
    }

    public async Task<GuardShiftCoverageRequirementDto> CreateAsync(CreateCoverageRequirementDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear requerimientos.");

        var entity = new GuardShiftCoverageRequirement
        {
            LocationId = dto.LocationId,
            ScheduleId = dto.ScheduleId,
            DayOfWeek = dto.DayOfWeek,
            RequiredGuards = dto.RequiredGuards,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Notes = dto.Notes,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.RequirementId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el requerimiento creado.");
    }

    public async Task<GuardShiftCoverageRequirementDto> UpdateAsync(int requirementId, UpdateCoverageRequirementDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardShiftCoverageRequirements
            .FirstOrDefaultAsync(r => r.RequirementId == requirementId, ct)
            ?? throw new KeyNotFoundException($"Requerimiento {requirementId} no encontrado.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar requerimientos.");

        entity.DayOfWeek = dto.DayOfWeek;
        entity.RequiredGuards = dto.RequiredGuards;
        entity.ValidFrom = dto.ValidFrom;
        entity.ValidTo = dto.ValidTo;
        entity.IsActive = dto.IsActive;
        entity.Notes = dto.Notes;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(requirementId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el requerimiento actualizado.");
    }

    private static GuardShiftCoverageRequirementDto MapToDto(GuardShiftCoverageRequirement r)
    {
        var dayNames = new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
        return new GuardShiftCoverageRequirementDto(
            r.RequirementId,
            r.LocationId,
            r.Location?.LocationName ?? string.Empty,
            r.ScheduleId,
            r.Schedule?.Description ?? string.Empty,
            r.DayOfWeek,
            r.DayOfWeek < dayNames.Length ? dayNames[r.DayOfWeek] : r.DayOfWeek.ToString(),
            r.RequiredGuards,
            r.ValidFrom,
            r.ValidTo,
            r.IsActive,
            r.Notes
        );
    }
}
