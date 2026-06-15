using Microsoft.EntityFrameworkCore;
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

    public AcademicLadderService(
        IAcademicLadderRepository repo,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AcademicLadderDto>> GetAllAsync(CancellationToken ct)
    {
        var items = await _repo.GetAllOrderedAsync(ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<AcademicLadderDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _db.AcademicLadders
            .Include(a => a.CategoryType)
            .Include(a => a.LevelType)
            .Include(a => a.DedicationType)
            .Include(a => a.NextLadder)
            .FirstOrDefaultAsync(a => a.LadderId == id, ct);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<AcademicLadderDto?> GetNextAsync(int currentLadderId, CancellationToken ct)
    {
        var next = await _repo.GetNextAsync(currentLadderId, ct);
        return next is null ? null : MapToDto(next);
    }

    public async Task<AcademicLadderDto> CreateAsync(AcademicLadderCreateDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear escalafones.");

        if (await _db.AcademicLadders.AnyAsync(a => a.Code == dto.Code, ct))
            throw new InvalidOperationException($"Ya existe un escalafón con el código '{dto.Code}'.");

        if (dto.NextLadderId.HasValue && !await _db.AcademicLadders.AnyAsync(a => a.LadderId == dto.NextLadderId, ct))
            throw new KeyNotFoundException($"El escalafón siguiente (Id={dto.NextLadderId}) no existe.");

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
            CreatedAt        = DateTime.UtcNow
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
        entity.UpdatedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Error al recuperar el escalafón actualizado.");
    }

    // CategoryTypeIds para Titulares según ref_Types ACADEMIC_CATEGORY
    private static readonly HashSet<int> TitularCategoryIds = [2012, 2013, 2014];

    private static AcademicLadderDto MapToDto(AcademicLadder a) => new(
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
        IsTitular: a.CategoryTypeId.HasValue && TitularCategoryIds.Contains(a.CategoryTypeId.Value)
    );
}
