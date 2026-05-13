using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardServiceLocationService : IGuardServiceLocationService
{
    private readonly IGuardServiceLocationRepository _repo;
    private readonly AppDbContext _db;

    public GuardServiceLocationService(IGuardServiceLocationRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<List<GuardServiceLocationTreeDto>> GetTreeAsync(CancellationToken ct)
    {
        var roots = await _repo.GetTreeAsync(ct);
        return roots.Select(MapToTree).ToList();
    }

    public async Task<List<GuardServiceLocationDto>> GetAssignableAsync(CancellationToken ct)
    {
        var locations = await _repo.GetAssignableAsync(ct);
        return locations.Select(l => MapToDto(l, null)).ToList();
    }

    public async Task<GuardServiceLocationDto?> GetByIdAsync(int locationId, CancellationToken ct)
    {
        var location = await _db.GuardServiceLocations
            .FirstOrDefaultAsync(l => l.LocationId == locationId, ct);
        return location is null ? null : MapToDto(location, null);
    }

    public async Task<GuardServiceLocationDto> CreateAsync(CreateGuardServiceLocationDto dto, CancellationToken ct)
    {
        int level = 0;
        int? rootId = null;

        if (dto.ParentLocationId.HasValue)
        {
            var parent = await _db.GuardServiceLocations
                .FirstOrDefaultAsync(l => l.LocationId == dto.ParentLocationId.Value, ct)
                ?? throw new InvalidOperationException("La ubicación padre no existe.");

            level = parent.Level + 1;
            rootId = dto.RootLocationId ?? parent.RootLocationId ?? parent.LocationId;
        }

        var entity = new GuardServiceLocation
        {
            ParentLocationId = dto.ParentLocationId,
            RootLocationId = rootId,
            LocationTypeId = dto.LocationTypeId,
            LocationCode = dto.LocationCode,
            LocationName = dto.LocationName,
            Description = dto.Description,
            Level = level,
            RequiresCoverage = dto.RequiresCoverage,
            IsAssignable = dto.IsAssignable,
            IsActive = true
        };

        await _repo.AddAsync(entity, ct);
        return MapToDto(entity, null);
    }

    public async Task<GuardServiceLocationDto> UpdateAsync(int locationId, UpdateGuardServiceLocationDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardServiceLocations
            .FirstOrDefaultAsync(l => l.LocationId == locationId, ct)
            ?? throw new KeyNotFoundException($"Ubicación {locationId} no encontrada.");

        entity.LocationTypeId = dto.LocationTypeId;
        entity.LocationCode = dto.LocationCode;
        entity.LocationName = dto.LocationName;
        entity.Description = dto.Description;
        entity.RequiresCoverage = dto.RequiresCoverage;
        entity.IsAssignable = dto.IsAssignable;
        entity.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);
        return MapToDto(entity, null);
    }

    private static GuardServiceLocationDto MapToDto(GuardServiceLocation l, List<GuardServiceLocationDto>? children) =>
        new(l.LocationId, l.ParentLocationId, l.RootLocationId, l.LocationTypeId,
            null, l.LocationCode, l.LocationName, l.Description,
            l.LocationPath, l.Level, l.RequiresCoverage, l.IsAssignable, l.IsActive, children);

    private static GuardServiceLocationTreeDto MapToTree(GuardServiceLocation l) =>
        new(l.LocationId, l.LocationName, l.LocationCode, l.Level,
            l.IsAssignable, l.RequiresCoverage, l.IsActive,
            l.Children.Select(MapToTree).ToList());
}
