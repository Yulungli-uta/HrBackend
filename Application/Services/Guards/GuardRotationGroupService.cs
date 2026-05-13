using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardRotationGroupService : IGuardRotationGroupService
{
    private readonly IGuardRotationGroupRepository _repo;
    private readonly AppDbContext _db;

    public GuardRotationGroupService(IGuardRotationGroupRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<List<GuardRotationGroupDto>> GetAllAsync(CancellationToken ct)
    {
        var groups = await _db.GuardRotationGroups
            .Select(g => new GuardRotationGroupDto(
                g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive,
                g.Employees.Count(e => e.IsActive)))
            .ToListAsync(ct);
        return groups;
    }

    public async Task<PagedResult<GuardRotationGroupDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.GuardRotationGroups.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(g => g.Name.ToLower().Contains(term) ||
                             (g.GroupCode != null && g.GroupCode.ToLower().Contains(term)));
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GuardRotationGroupDto(
                g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive,
                g.Employees.Count(e => e.IsActive)))
            .ToListAsync(ct);

        return new PagedResult<GuardRotationGroupDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardRotationGroupDto?> GetByIdAsync(int groupId, CancellationToken ct)
    {
        var g = await _db.GuardRotationGroups
            .Include(x => x.Employees.Where(e => e.IsActive))
            .FirstOrDefaultAsync(x => x.GroupId == groupId, ct);
        if (g is null) return null;
        return new GuardRotationGroupDto(g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive, g.Employees.Count);
    }

    public async Task<GuardRotationGroupDto> CreateAsync(CreateGuardRotationGroupDto dto, CancellationToken ct)
    {
        var entity = new GuardRotationGroup
        {
            GroupCode = dto.GroupCode,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };
        await _repo.AddAsync(entity, ct);
        return new GuardRotationGroupDto(entity.GroupId, entity.GroupCode, entity.Name, entity.Description, entity.IsActive, 0);
    }

    public async Task<GuardRotationGroupDto> UpdateAsync(int groupId, UpdateGuardRotationGroupDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardRotationGroups.FirstOrDefaultAsync(g => g.GroupId == groupId, ct)
            ?? throw new KeyNotFoundException($"Grupo {groupId} no encontrado.");
        entity.GroupCode = dto.GroupCode;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        return new GuardRotationGroupDto(entity.GroupId, entity.GroupCode, entity.Name, entity.Description, entity.IsActive, 0);
    }

    public async Task<List<GuardRotationGroupEmployeeDto>> GetEmployeesAsync(int groupId, CancellationToken ct)
    {
        var group = await _repo.GetWithEmployeesAsync(groupId, ct)
            ?? throw new KeyNotFoundException($"Grupo {groupId} no encontrado.");

        return group.Employees.Select(e => new GuardRotationGroupEmployeeDto(
            e.GroupEmployeeId, e.GroupId, group.Name, e.EmployeeId,
            $"{e.Employee?.People?.FirstName} {e.Employee?.People?.LastName}",
            e.Employee?.People?.IdCard,
            e.ValidFrom, e.ValidTo, e.IsActive, e.Notes
        )).ToList();
    }

    public async Task<GuardRotationGroupEmployeeDto> AssignEmployeeAsync(int groupId, AssignEmployeeToRotationGroupDto dto, CancellationToken ct)
    {
        var group = await _db.GuardRotationGroups.FirstOrDefaultAsync(g => g.GroupId == groupId, ct)
            ?? throw new KeyNotFoundException($"Grupo {groupId} no encontrado.");

        var employee = await _db.Set<WsUtaSystem.Models.Employees>()
            .Include(e => e.People)
            .FirstOrDefaultAsync(e => e.EmployeeId == dto.EmployeeId && e.IsActive, ct)
            ?? throw new InvalidOperationException("Empleado no encontrado o inactivo.");

        var entity = new GuardRotationGroupEmployee
        {
            GroupId = groupId,
            EmployeeId = dto.EmployeeId,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Notes = dto.Notes,
            IsActive = true
        };
        await _db.GuardRotationGroupEmployees.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return new GuardRotationGroupEmployeeDto(
            entity.GroupEmployeeId, groupId, group.Name, dto.EmployeeId,
            $"{employee.People?.FirstName} {employee.People?.LastName}",
            employee.People?.IdCard,
            entity.ValidFrom, entity.ValidTo, entity.IsActive, entity.Notes
        );
    }

    public async Task RemoveEmployeeAsync(int groupId, RemoveEmployeeFromRotationGroupDto dto, CancellationToken ct)
    {
        var entry = await _db.GuardRotationGroupEmployees
            .FirstOrDefaultAsync(e => e.GroupEmployeeId == dto.GroupEmployeeId && e.GroupId == groupId, ct)
            ?? throw new KeyNotFoundException("Asignación no encontrada.");
        entry.ValidTo = dto.ValidTo;
        entry.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<LocationSummaryDto>> GetLocationSummaryAsync(CancellationToken ct)
    {
        var groups = await _db.GuardRotationGroups
            .Include(g => g.Employees.Where(e => e.IsActive))
            .Include(g => g.Patterns.Where(p => p.IsActive))
            .ToListAsync(ct);

        return groups
            .GroupBy(g => ExtractLocationKey(g.GroupCode))
            .Select(grp => new LocationSummaryDto(
                LocationKey: grp.Key,
                LocationName: grp.Key.Replace("_", " "),
                TotalGroups: grp.Count(),
                TotalActiveGroups: grp.Count(g => g.IsActive),
                TotalEmployees: grp.Sum(g => g.Employees.Count),
                TotalPatterns: grp.SelectMany(g => g.Patterns).Select(p => p.PatternId).Distinct().Count()
            ))
            .OrderBy(s => s.LocationName)
            .ToList();
    }

    public async Task<List<LocationGroupDetailDto>> GetByLocationKeyAsync(string locationKey, CancellationToken ct)
    {
        var allGroups = await _db.GuardRotationGroups
            .Include(g => g.Employees.Where(e => e.IsActive))
            .Include(g => g.Patterns.Where(p => p.IsActive))
                .ThenInclude(gp => gp.Pattern)
            .ToListAsync(ct);

        return allGroups
            .Where(g => ExtractLocationKey(g.GroupCode) == locationKey)
            .Select(g =>
            {
                var activePattern = g.Patterns.FirstOrDefault(p => p.IsActive);
                var sequence = ExtractPatternSequence(g.GroupCode);
                return new LocationGroupDetailDto(
                    GroupId: g.GroupId,
                    GroupCode: g.GroupCode,
                    GroupName: g.Name,
                    Description: g.Description,
                    IsActive: g.IsActive,
                    PatternId: activePattern?.PatternId,
                    PatternCode: activePattern?.Pattern?.PatternCode,
                    PatternName: activePattern?.Pattern?.Name,
                    PatternSequence: sequence,
                    PatternReadable: BuildPatternReadable(sequence),
                    AssignedEmployees: g.Employees.Count
                );
            })
            .OrderBy(d => d.GroupCode)
            .ToList();
    }

    private static string ExtractLocationKey(string? groupCode)
    {
        if (string.IsNullOrWhiteSpace(groupCode)) return "SIN_UBICACION";
        var parts = groupCode.Split('_');
        return parts.Length >= 3 ? parts[2] : "OTROS";
    }

    private static string ExtractPatternSequence(string? groupCode)
    {
        if (string.IsNullOrWhiteSpace(groupCode)) return string.Empty;
        var parts = groupCode.Split('_');
        return parts.Length >= 4 ? string.Concat(parts.Skip(3)) : string.Empty;
    }

    private static readonly Dictionary<char, string> _seqMap = new()
    {
        ['L'] = "Libre", ['M'] = "Mañana", ['T'] = "Tarde", ['N'] = "Noche"
    };

    private static string BuildPatternReadable(string sequence)
    {
        if (string.IsNullOrEmpty(sequence)) return string.Empty;
        return string.Join("-", sequence.Select(c =>
            _seqMap.TryGetValue(char.ToUpper(c), out var name) ? name : c.ToString()));
    }
}
