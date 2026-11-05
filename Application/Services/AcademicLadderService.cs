using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.AcademicLadder;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class AcademicLadderService : IAcademicLadderService
{
    private readonly IAcademicLadderRepository _repo;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    private readonly IAppClock _clock;

    public AcademicLadderService(
        IAcademicLadderRepository repo,
        AppDbContext db,
        ICurrentUserService currentUser,
        IAppClock clock)
    {
        _clock = clock;
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AcademicLadderDto>> GetAllAsync(CancellationToken ct)
    {
        var items = await _repo.GetAllOrderedAsync(ct);
        var teachers = await CountActiveTeachersAsync(ct);
        return items.Select(a => MapToDto(a, teachers.GetValueOrDefault(a.LadderId))).ToList();
    }

    /// <summary>Docentes con estructura activa agrupados por escalón (una sola consulta para todo el catálogo).</summary>
    private async Task<Dictionary<int, int>> CountActiveTeachersAsync(CancellationToken ct) =>
        await _db.TeacherStructures.AsNoTracking()
            .Where(t => t.IsActive && t.LadderId != null)
            .GroupBy(t => t.LadderId!.Value)
            .Select(g => new { LadderId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.LadderId, x => x.Total, ct);

    public async Task<AcademicLadderDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _db.AcademicLadders
            .Include(a => a.CategoryType)
            .Include(a => a.LevelType)
            .Include(a => a.DedicationType)
            .Include(a => a.NextLadder)
            .FirstOrDefaultAsync(a => a.LadderId == id, ct);

        if (entity is null) return null;

        var activeTeachers = await _db.TeacherStructures.AsNoTracking()
            .CountAsync(t => t.IsActive && t.LadderId == id, ct);
        return MapToDto(entity, activeTeachers);
    }

    public async Task<AcademicLadderDto?> GetNextAsync(int currentLadderId, CancellationToken ct)
    {
        var next = await _repo.GetNextAsync(currentLadderId, ct);
        return next is null ? null : MapToDto(next, 0);
    }

    public async Task<AcademicLadderDto> CreateAsync(AcademicLadderCreateDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear escalafones.");

        if (await _db.AcademicLadders.AnyAsync(a => a.Code == dto.Code, ct))
            throw new InvalidOperationException($"Ya existe un escalafón con el código '{dto.Code}'.");

        if (dto.NextLadderId.HasValue && !await _db.AcademicLadders.AnyAsync(a => a.LadderId == dto.NextLadderId, ct))
            throw new KeyNotFoundException($"El escalafón siguiente (Id={dto.NextLadderId}) no existe.");

        await ValidateTypeReferencesAsync(dto.CategoryTypeId, dto.LevelTypeId, dto.DedicationTypeId, ct);

        var entity = new AcademicLadder
        {
            Code             = dto.Code.Trim().ToUpper(),
            Name             = dto.Name,
            CategoryTypeId   = dto.CategoryTypeId,
            LevelTypeId      = dto.LevelTypeId,
            DedicationTypeId = dto.DedicationTypeId,
            BaseRmu          = dto.BaseRmu,
            Sequence         = dto.Sequence,
            NextLadderId     = dto.NextLadderId,
            MinYearsService  = dto.MinYearsService,
            IsActive         = true,
            CreatedBy        = userId,
            CreatedAt        = _clock.Now
        };

        await _repo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.LadderId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el escalafón creado.");
    }

    public async Task<AcademicLadderDto> UpdateAsync(int id, AcademicLadderUpdateDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar escalafones.");

        var entity = await _db.AcademicLadders.FirstOrDefaultAsync(a => a.LadderId == id, ct)
            ?? throw new KeyNotFoundException($"Escalafón {id} no encontrado.");

        if (dto.NextLadderId.HasValue)
        {
            if (dto.NextLadderId == id)
                throw new InvalidOperationException("Un escalafón no puede apuntar a sí mismo como siguiente.");
            if (!await _db.AcademicLadders.AnyAsync(a => a.LadderId == dto.NextLadderId, ct))
                throw new KeyNotFoundException($"El escalafón siguiente (Id={dto.NextLadderId}) no existe.");
        }

        // Red de seguridad contra el borrado accidental: un cliente que no envía la categoría
        // (formulario antiguo o payload parcial) no debe dejar el escalón sin categoría.
        if (entity.CategoryTypeId.HasValue && !dto.CategoryTypeId.HasValue)
            throw new InvalidOperationException(
                "La categoría del escalón no puede quedar vacía. Envíe la categoría actual o seleccione una nueva.");

        await ValidateTypeReferencesAsync(dto.CategoryTypeId, dto.LevelTypeId, dto.DedicationTypeId, ct);

        entity.Name             = dto.Name;
        entity.CategoryTypeId   = dto.CategoryTypeId;
        entity.LevelTypeId      = dto.LevelTypeId;
        entity.DedicationTypeId = dto.DedicationTypeId;
        entity.BaseRmu          = dto.BaseRmu;
        entity.Sequence         = dto.Sequence;
        entity.NextLadderId     = dto.NextLadderId;
        entity.MinYearsService  = dto.MinYearsService;
        entity.IsActive         = dto.IsActive;
        entity.UpdatedBy        = userId;
        entity.UpdatedAt        = _clock.Now;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Error al recuperar el escalafón actualizado.");
    }

    /// <summary>
    /// Verifica que cada Id exista, esté activo y pertenezca a la categoría de catálogo esperada
    /// (ACADEMIC_CATEGORY / ACADEMIC_LEVEL / ACADEMIC_DEDICATION), para no guardar un tipo de otro catálogo.
    /// </summary>
    private async Task ValidateTypeReferencesAsync(int? categoryId, int? levelId, int? dedicationId, CancellationToken ct)
    {
        await EnsureTypeAsync(categoryId, "ACADEMIC_CATEGORY", "categoría", ct);
        await EnsureTypeAsync(levelId, "ACADEMIC_LEVEL", "nivel", ct);
        await EnsureTypeAsync(dedicationId, "ACADEMIC_DEDICATION", "dedicación", ct);
    }

    private async Task EnsureTypeAsync(int? typeId, string category, string label, CancellationToken ct)
    {
        if (!typeId.HasValue) return;

        var ok = await _db.RefTypes.AsNoTracking()
            .AnyAsync(r => r.TypeId == typeId.Value && r.Category == category && r.IsActive, ct);
        if (!ok)
            throw new InvalidOperationException($"La {label} indicada (Id={typeId}) no es válida o está inactiva.");
    }

    private static AcademicLadderDto MapToDto(AcademicLadder a, int activeTeachers) => new(
        a.LadderId,
        a.Code,
        a.Name,
        a.CategoryTypeId,
        a.CategoryType?.Name,
        a.LevelTypeId,
        a.LevelType?.Name,
        a.DedicationTypeId,
        a.DedicationType?.Name,
        a.BaseRmu,
        a.Sequence,
        a.NextLadderId,
        a.NextLadder?.Name,
        a.MinYearsService,
        a.IsActive,
        IsTitular: AcademicCategories.IsTitular(a.CategoryType?.Name),
        ActiveTeachers: activeTeachers
    );
}
