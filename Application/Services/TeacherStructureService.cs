using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class TeacherStructureService : ITeacherStructureService
{
    private readonly ITeacherStructureRepository _repo;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TeacherStructureService(
        ITeacherStructureRepository repo,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<TeacherStructureDto>> GetPagedAsync(TeacherStructureFilterDto filter, CancellationToken ct)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var q = _db.TeacherStructures
            .Include(t => t.Employee).ThenInclude(e => e!.People)
            .Include(t => t.Ladder)
            .Include(t => t.DedicationType)
            .Include(t => t.Department)
            .AsQueryable();

        if (filter.EmployeeId.HasValue)     q = q.Where(t => t.EmployeeId == filter.EmployeeId.Value);
        if (filter.DedicationTypeId.HasValue) q = q.Where(t => t.DedicationTypeId == filter.DedicationTypeId.Value);
        if (filter.LadderId.HasValue)        q = q.Where(t => t.LadderId == filter.LadderId.Value);
        if (filter.DepartmentId.HasValue)    q = q.Where(t => t.DepartmentId == filter.DepartmentId.Value);
        if (filter.IsActive.HasValue)        q = q.Where(t => t.IsActive == filter.IsActive.Value);

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderByDescending(t => t.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TeacherStructureDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<List<TeacherStructureDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        var items = await _repo.GetByEmployeeAsync(employeeId, ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<TeacherStructureDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _db.TeacherStructures
            .Include(t => t.Employee).ThenInclude(e => e!.People)
            .Include(t => t.Ladder)
            .Include(t => t.DedicationType)
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.TeacherStructureId == id, ct);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<TeacherStructureDto> CreateAsync(TeacherStructureCreateDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear estructuras docentes.");

        if (!await _db.Employees.AnyAsync(e => e.EmployeeId == dto.EmployeeId && e.IsActive, ct))
            throw new KeyNotFoundException($"Empleado {dto.EmployeeId} no encontrado o inactivo.");

        if (await _repo.HasOverlapAsync(dto.EmployeeId, dto.StartDate, dto.EndDate, null, ct))
            throw new InvalidOperationException("Ya existe una estructura docente activa que se solapa con el período indicado.");

        var entity = new TeacherStructure
        {
            EmployeeId      = dto.EmployeeId,
            LadderId        = dto.LadderId,
            DedicationTypeId = dto.DedicationTypeId,
            WeeklyClassHours = dto.WeeklyClassHours,
            HourValue       = dto.HourValue,
            Rmu             = dto.Rmu,
            DepartmentId    = dto.DepartmentId,
            StartDate       = dto.StartDate,
            EndDate         = dto.EndDate,
            IsActive        = true,
            CreatedBy       = userId,
            CreatedAt       = DateTime.UtcNow
        };

        await _repo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.TeacherStructureId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la estructura docente creada.");
    }

    public async Task<TeacherStructureDto> UpdateAsync(int id, TeacherStructureUpdateDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar estructuras docentes.");

        var entity = await _db.TeacherStructures.FirstOrDefaultAsync(t => t.TeacherStructureId == id, ct)
            ?? throw new KeyNotFoundException($"Estructura docente {id} no encontrada.");

        if (await _repo.HasOverlapAsync(entity.EmployeeId, dto.StartDate, dto.EndDate, id, ct))
            throw new InvalidOperationException("Ya existe una estructura docente activa que se solapa con el período indicado.");

        entity.LadderId         = dto.LadderId;
        entity.DedicationTypeId = dto.DedicationTypeId;
        entity.WeeklyClassHours = dto.WeeklyClassHours;
        entity.HourValue        = dto.HourValue;
        entity.Rmu              = dto.Rmu;
        entity.DepartmentId     = dto.DepartmentId;
        entity.StartDate        = dto.StartDate;
        entity.EndDate          = dto.EndDate;
        entity.EligiblePromotion  = dto.EligiblePromotion;
        entity.EligibleRecategory = dto.EligibleRecategory;
        entity.EligibleDedicChg   = dto.EligibleDedicChg;
        entity.UpdatedBy        = userId;
        entity.UpdatedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Error al recuperar la estructura docente actualizada.");
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede inactivar estructuras docentes.");

        var entity = await _db.TeacherStructures.FirstOrDefaultAsync(t => t.TeacherStructureId == id, ct)
            ?? throw new KeyNotFoundException($"Estructura docente {id} no encontrada.");

        entity.IsActive   = false;
        entity.EndDate    = entity.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        entity.UpdatedBy  = userId;
        entity.UpdatedAt  = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // ─── Mapper ───────────────────────────────────────────────────────────────

    private static TeacherStructureDto MapToDto(TeacherStructure t) => new(
        t.TeacherStructureId,
        t.EmployeeId,
        t.Employee is null ? string.Empty : $"{t.Employee.People?.FirstName} {t.Employee.People?.LastName}".Trim(),
        t.Employee?.People?.IdCard,
        t.LadderId,
        t.Ladder?.Name,
        t.DedicationTypeId,
        t.DedicationType?.Name ?? string.Empty,
        t.WeeklyClassHours,
        t.HourValue,
        t.Rmu,
        t.DepartmentId,
        t.Department?.Name,
        t.StartDate,
        t.EndDate,
        t.IsActive,
        t.EligiblePromotion,
        t.EligibleRecategory,
        t.EligibleDedicChg
    );
}
