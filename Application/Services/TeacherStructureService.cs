using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Extensions;
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

    private readonly IAppClock _clock;

    public TeacherStructureService(
        ITeacherStructureRepository repo,
        AppDbContext db,
        ICurrentUserService currentUser,
        IAppClock clock)
    {
        _clock = clock;
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
            CreatedAt       = _clock.Now
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
        entity.UpdatedAt        = _clock.Now;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Error al recuperar la estructura docente actualizada.");
    }

    public async Task<TeacherStructureDto> ApplyChangeAsync(int id, TeacherStructureApplyChangeDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar estructuras docentes.");

        var reason = dto.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5)
            throw new InvalidOperationException("Debe indicar el motivo o documento que respalda el cambio (mínimo 5 caracteres).");
        if (reason.Length > 500)
            throw new InvalidOperationException("El motivo no puede superar los 500 caracteres.");

        var current = await _db.TeacherStructures.FirstOrDefaultAsync(t => t.TeacherStructureId == id, ct)
            ?? throw new KeyNotFoundException($"Estructura docente {id} no encontrada.");
        if (!current.IsActive)
            throw new InvalidOperationException("Solo se puede actualizar la estructura docente vigente del empleado.");

        var today = _clock.Today;
        if (dto.EffectiveDate > today)
            throw new InvalidOperationException("La fecha efectiva no puede ser futura: la estructura nueva rige desde ese día.");
        if (dto.EffectiveDate <= current.StartDate)
            throw new InvalidOperationException(
                $"La fecha efectiva debe ser posterior al inicio de la estructura vigente ({current.StartDate:dd/MM/yyyy}).");

        var ladder = await _db.AcademicLadders.AsNoTracking()
            .Include(l => l.CategoryType)
            .FirstOrDefaultAsync(l => l.LadderId == dto.NewLadderId, ct)
            ?? throw new KeyNotFoundException($"El escalón {dto.NewLadderId} no existe.");
        if (!ladder.IsActive)
            throw new InvalidOperationException("El escalón seleccionado está inactivo.");
        if (!AcademicCategories.IsTitular(ladder.CategoryType?.Name))
            throw new InvalidOperationException("Este cambio solo admite escalones de docentes titulares.");

        var dedication = await _db.RefTypes.AsNoTracking()
            .FirstOrDefaultAsync(r => r.TypeId == dto.NewDedicationTypeId && r.Category == "ACADEMIC_DEDICATION" && r.IsActive, ct)
            ?? throw new InvalidOperationException($"La dedicación indicada (Id={dto.NewDedicationTypeId}) no es válida o está inactiva.");
        if (ladder.DedicationTypeId.HasValue && ladder.DedicationTypeId.Value != dto.NewDedicationTypeId)
            throw new InvalidOperationException("La dedicación no coincide con la del escalón seleccionado.");

        var hours = dto.WeeklyClassHours ?? dedication.Name switch
        {
            "TC" => 40m,
            "MT" => 20m,
            _ => (decimal?)null,
        };
        if (!hours.HasValue || hours.Value <= 0 || hours.Value > 60)
            throw new InvalidOperationException("Indique las horas semanales (mayores a 0 y hasta 60).");

        var departmentId = dto.DepartmentId ?? current.DepartmentId;
        if (dto.DepartmentId.HasValue && !await _db.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId.Value, ct))
            throw new KeyNotFoundException($"La dependencia {dto.DepartmentId} no existe.");

        var unchanged = current.LadderId == dto.NewLadderId
                        && current.DedicationTypeId == dto.NewDedicationTypeId
                        && current.WeeklyClassHours == hours
                        && current.DepartmentId == departmentId;
        if (unchanged)
            throw new InvalidOperationException("No hay cambios: el escalón, la dedicación, las horas y la dependencia son los mismos.");

        if (await _repo.HasOverlapAsync(current.EmployeeId, dto.EffectiveDate, null, current.TeacherStructureId, ct))
            throw new InvalidOperationException("Ya existe otra estructura docente activa que se solapa con la fecha indicada.");

        var now = _clock.Now;
        var before = new
        {
            current.LadderId, current.DedicationTypeId, current.WeeklyClassHours, current.DepartmentId,
            StartDate = current.StartDate.ToString("yyyy-MM-dd"),
        };

        // Se cierra la vigente el día anterior; el historial queda intacto.
        current.IsActive  = false;
        current.EndDate   = dto.EffectiveDate.AddDays(-1);
        current.UpdatedBy = userId;
        current.UpdatedAt = now;

        // El RMU no se copia: el sueldo vive en el libro (HR.tbl_SalaryHistory). Los datos SIIES de categoría solo se
        // conservan si el escalón no cambió; con otro escalón se recalculan desde Ladder.SiiesLabel.
        var next = new TeacherStructure
        {
            EmployeeId       = current.EmployeeId,
            LadderId         = dto.NewLadderId,
            DedicationTypeId = dto.NewDedicationTypeId,
            WeeklyClassHours = hours,
            HourValue        = current.HourValue,
            DepartmentId     = departmentId,
            StartDate        = dto.EffectiveDate,
            IsActive         = true,
            SiiesTipoEscalafonNombramientoTypeId = current.SiiesTipoEscalafonNombramientoTypeId,
            SiiesNivelTypeId                     = current.SiiesNivelTypeId,
            SiiesCategoriaTypeId                 = current.LadderId == dto.NewLadderId ? current.SiiesCategoriaTypeId : null,
            CreatedBy = userId,
            CreatedAt = now,
        };
        _db.TeacherStructures.Add(next);

        _db.Set<Audit>().Add(new Audit
        {
            TableName = "TeacherStructure",
            Action    = "SCALE_CHANGE",
            RecordId  = id.ToString(),
            UserName  = string.IsNullOrWhiteSpace(_currentUser.UserName ?? _currentUser.Email) ? "unknown" : (_currentUser.UserName ?? _currentUser.Email)!,
            DateTime  = now,
            Details   = System.Text.Json.JsonSerializer.Serialize(new
            {
                Reason = reason,
                EmployeeId = current.EmployeeId,
                EffectiveDate = dto.EffectiveDate.ToString("yyyy-MM-dd"),
                Before = before,
                After = new { dto.NewLadderId, dto.NewDedicationTypeId, WeeklyClassHours = hours, DepartmentId = departmentId },
            }),
        });

        // Un solo SaveChanges: cerrar la vigente, crear la nueva y auditar son atómicos.
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(next.TeacherStructureId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la estructura docente creada.");
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede inactivar estructuras docentes.");

        var entity = await _db.TeacherStructures.FirstOrDefaultAsync(t => t.TeacherStructureId == id, ct)
            ?? throw new KeyNotFoundException($"Estructura docente {id} no encontrada.");

        entity.IsActive   = false;
        entity.EndDate    = entity.EndDate ?? _clock.Today;
        entity.UpdatedBy  = userId;
        entity.UpdatedAt  = _clock.Now;

        await _db.SaveChangesAsync(ct);
    }

    // ─── Mapper ───────────────────────────────────────────────────────────────

    private static TeacherStructureDto MapToDto(TeacherStructure t) => new(
        t.TeacherStructureId,
        t.EmployeeId,
        t.Employee is null ? string.Empty : t.Employee.People.GetFullName(),
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
