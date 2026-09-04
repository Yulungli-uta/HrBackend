using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Extensions;
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

    // Cargos (HR.tbl_jobs.Description) que califican como "guardia" para el buscador de
    // "Agregar guardias". Constante en código (no tabla de catálogo), mismo patrón ya usado
    // en el proyecto (ver comentario en tbl_EmployeeCertificateRequests / CertificateType).
    // Si se crea un cargo nuevo de guardia (ej. "Vigilante"), agregarlo aquí.
    private static readonly string[] GuardJobNames =
    {
        "Guardia", "Guardián", "Guardián/Guardián Administrativo", "Guardias de Seguridad", "Supervisor"
    };

    public GuardRotationGroupService(IGuardRotationGroupRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<List<EligibleEmployeeDto>> GetEligibleEmployeesAsync(string? search, CancellationToken ct)
    {
        var query = _db.Set<WsUtaSystem.Models.Employees>()
            .AsNoTracking()
            .Include(e => e.People)
            .Where(e => e.IsActive && e.JobId != null);

        var jobIds = _db.Set<WsUtaSystem.Models.Job>()
            .AsNoTracking()
            .Where(j => j.Description != null && GuardJobNames.Contains(j.Description));

        query = query.Where(e => jobIds.Any(j => j.JobID == e.JobId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                (e.People!.LastName + " " + e.People.FirstName).ToLower().Contains(term) ||
                (e.People.IdCard != null && e.People.IdCard.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(e => e.People!.LastName).ThenBy(e => e.People!.FirstName)
            .Take(20)
            .Select(e => new EligibleEmployeeDto(
                e.EmployeeId,
                e.People!.LastName + " " + e.People.FirstName,
                e.People.IdCard,
                e.Email ?? e.People.Email))
            .ToListAsync(ct);
    }

    public async Task<List<GuardRotationGroupDto>> GetAllAsync(CancellationToken ct) =>
        await _db.GuardRotationGroups
            .OrderBy(g => g.Name)
            .Select(g => new GuardRotationGroupDto(
                g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive,
                g.Employees.Count(e => e.IsActive),
                g.ParentGroupId,
                g.ParentGroup == null ? null : g.ParentGroup.Name,
                g.GroupLevelType == null ? null : g.GroupLevelType.Name,
                g.ColorCode,
                g.Subgroups.Count(s => s.IsActive), g.IsSpecial))
            .ToListAsync(ct);

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
                g.Employees.Count(e => e.IsActive),
                g.ParentGroupId,
                g.ParentGroup == null ? null : g.ParentGroup.Name,
                g.GroupLevelType == null ? null : g.GroupLevelType.Name,
                g.ColorCode,
                g.Subgroups.Count(s => s.IsActive), g.IsSpecial))
            .ToListAsync(ct);

        return new PagedResult<GuardRotationGroupDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardRotationGroupDto?> GetByIdAsync(int groupId, CancellationToken ct) =>
        await _db.GuardRotationGroups
            .Where(g => g.GroupId == groupId)
            .Select(g => new GuardRotationGroupDto(
                g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive,
                g.Employees.Count(e => e.IsActive),
                g.ParentGroupId,
                g.ParentGroup == null ? null : g.ParentGroup.Name,
                g.GroupLevelType == null ? null : g.GroupLevelType.Name,
                g.ColorCode,
                g.Subgroups.Count(s => s.IsActive), g.IsSpecial))
            .FirstOrDefaultAsync(ct);

    public async Task<GuardRotationGroupDto> CreateAsync(CreateGuardRotationGroupDto dto, CancellationToken ct)
    {
        var entity = new GuardRotationGroup
        {
            GroupCode = dto.GroupCode,
            Name = dto.Name,
            Description = dto.Description,
            ParentGroupId = dto.ParentGroupId,
            GroupLevelTypeId = dto.GroupLevelTypeId,
            ColorCode = dto.ColorCode,
            IsSpecial = dto.IsSpecial,
            IsActive = true
        };

        try
        {
            await _repo.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException(
                $"Ya existe un grupo activo con el código \"{dto.GroupCode}\". Inactiva el grupo anterior o elige otro código.");
        }

        return await GetByIdAsync(entity.GroupId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el grupo creado.");
    }

    public async Task<GuardRotationGroupDto> UpdateAsync(int groupId, UpdateGuardRotationGroupDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardRotationGroups.FirstOrDefaultAsync(g => g.GroupId == groupId, ct)
            ?? throw new KeyNotFoundException($"Grupo {groupId} no encontrado.");
        entity.GroupCode = dto.GroupCode;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        entity.ParentGroupId = dto.ParentGroupId;
        entity.GroupLevelTypeId = dto.GroupLevelTypeId;
        entity.ColorCode = dto.ColorCode;
        entity.IsSpecial = dto.IsSpecial;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException(
                $"Ya existe un grupo activo con el código \"{dto.GroupCode}\". Inactiva el otro grupo o elige otro código.");
        }

        return await GetByIdAsync(groupId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el grupo actualizado.");
    }

    public async Task<GuardRotationGroupDto> DuplicateAsync(int baseGroupId, DuplicateGuardRotationGroupDto dto, CancellationToken ct)
    {
        var baseGroup = await _db.GuardRotationGroups
            .Include(g => g.Employees.Where(e => e.IsActive))
            .FirstOrDefaultAsync(g => g.GroupId == baseGroupId, ct)
            ?? throw new KeyNotFoundException($"Grupo {baseGroupId} no encontrado.");

        var newGroup = new GuardRotationGroup
        {
            GroupCode = dto.NewGroupCode,
            Name = dto.NewName,
            Description = baseGroup.Description,
            ParentGroupId = dto.ParentGroupIdOverride ?? baseGroup.ParentGroupId,
            GroupLevelTypeId = baseGroup.GroupLevelTypeId,
            ColorCode = baseGroup.ColorCode,
            IsSpecial = baseGroup.IsSpecial,
            IsActive = true
        };

        try
        {
            await _repo.AddAsync(newGroup, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException(
                $"Ya existe un grupo activo con el código \"{dto.NewGroupCode}\". Elige otro código.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var emp in baseGroup.Employees)
        {
            await _db.GuardRotationGroupEmployees.AddAsync(new GuardRotationGroupEmployee
            {
                GroupId = newGroup.GroupId,
                EmployeeId = emp.EmployeeId,
                ValidFrom = today,
                ValidTo = null,
                IsActive = true,
                Notes = $"Duplicado desde grupo \"{baseGroup.Name}\" (#{baseGroup.GroupId})"
            }, ct);
        }
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(newGroup.GroupId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el grupo duplicado.");
    }

    public async Task<List<GuardRotationGroupEmployeeDto>> GetEmployeesAsync(int groupId, CancellationToken ct)
    {
        var group = await _repo.GetWithEmployeesAsync(groupId, ct)
            ?? throw new KeyNotFoundException($"Grupo {groupId} no encontrado.");

        return group.Employees.Select(e => new GuardRotationGroupEmployeeDto(
            e.GroupEmployeeId, e.GroupId, group.Name, e.EmployeeId,
            e.Employee?.People.GetFullName() ?? string.Empty,
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
            employee.People.GetFullName(),
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
                TotalEmployees: grp.SelectMany(g => g.Employees).Select(e => e.EmployeeId).Distinct().Count(),
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
                    AssignedEmployees: g.Employees.Count,
                    IsSpecial: g.IsSpecial
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

    public async Task<List<GuardGroupRotationPatternDto>> GetGroupPatternsAsync(int groupId, CancellationToken ct)
    {
        return await _db.GuardGroupRotationPatterns
            .Include(gp => gp.Pattern)
            .Where(gp => gp.GroupId == groupId)
            .OrderByDescending(gp => gp.IsActive)
            .ThenByDescending(gp => gp.ValidFrom)
            .Select(gp => new GuardGroupRotationPatternDto(
                gp.GroupPatternId, gp.GroupId, gp.PatternId,
                gp.Pattern!.Name, gp.Pattern.PatternCode,
                gp.StartCycleDate, gp.ValidFrom, gp.ValidTo, gp.IsActive, gp.Notes
            ))
            .ToListAsync(ct);
    }

    public async Task<GuardGroupRotationPatternDto> AssignPatternToGroupAsync(int groupId, AssignPatternToGroupDto dto, CancellationToken ct)
    {
        _ = await _db.GuardRotationGroups.FirstOrDefaultAsync(g => g.GroupId == groupId, ct)
            ?? throw new KeyNotFoundException($"Grupo {groupId} no encontrado.");

        var pattern = await _db.RotationPatterns
            .FirstOrDefaultAsync(p => p.PatternId == dto.PatternId && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Patrón {dto.PatternId} no encontrado o inactivo.");

        var overlappingAssignment = await _db.GuardGroupRotationPatterns
            .Include(gp => gp.Group)
            .Where(gp => gp.PatternId == dto.PatternId
                         && gp.GroupId != groupId
                         && gp.IsActive
                         && gp.Group != null
                         && gp.Group.IsActive
                         && gp.ValidFrom <= (dto.ValidTo ?? DateOnly.MaxValue)
                         && (gp.ValidTo ?? DateOnly.MaxValue) >= dto.ValidFrom)
            .OrderBy(gp => gp.ValidFrom)
            .FirstOrDefaultAsync(ct);

        if (overlappingAssignment is not null)
        {
            var groupName = overlappingAssignment.Group?.Name ?? $"Grupo {overlappingAssignment.GroupId}";
            throw new InvalidOperationException(
                $"El patron '{pattern.Name}' ya esta vigente en el grupo '{groupName}' para un rango de fechas que se cruza.");
        }

        var existing = await _db.GuardGroupRotationPatterns
            .Where(gp => gp.GroupId == groupId && gp.IsActive)
            .ToListAsync(ct);

        foreach (var gp in existing)
        {
            gp.IsActive = false;
            gp.ValidTo ??= dto.ValidFrom.AddDays(-1);
        }

        var entity = new GuardGroupRotationPattern
        {
            GroupId = groupId,
            PatternId = dto.PatternId,
            StartCycleDate = dto.StartCycleDate,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            IsActive = true,
            Notes = dto.Notes
        };

        await _db.GuardGroupRotationPatterns.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return new GuardGroupRotationPatternDto(
            entity.GroupPatternId, entity.GroupId, entity.PatternId,
            pattern.Name, pattern.PatternCode,
            entity.StartCycleDate, entity.ValidFrom, entity.ValidTo, entity.IsActive, entity.Notes
        );
    }

    public async Task RemovePatternFromGroupAsync(int groupId, int groupPatternId, CancellationToken ct)
    {
        var entity = await _db.GuardGroupRotationPatterns
            .FirstOrDefaultAsync(gp => gp.GroupPatternId == groupPatternId && gp.GroupId == groupId, ct)
            ?? throw new KeyNotFoundException("Asignación de patrón no encontrada.");

        entity.IsActive = false;
        entity.ValidTo ??= DateOnly.FromDateTime(DateTime.Today);
        await _db.SaveChangesAsync(ct);
    }

    // ─── Jerarquía de grupos ──────────────────────────────────────────────────

    public async Task<List<GuardRotationGroupDto>> GetGeneralGroupsAsync(CancellationToken ct) =>
        await _db.GuardRotationGroups
            .Where(g => g.ParentGroupId == null)
            .OrderBy(g => g.Name)
            .Select(g => new GuardRotationGroupDto(
                g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive,
                g.Employees.Count(e => e.IsActive),
                null, null,
                g.GroupLevelType == null ? null : g.GroupLevelType.Name,
                g.ColorCode,
                g.Subgroups.Count(s => s.IsActive), g.IsSpecial))
            .ToListAsync(ct);

    public async Task<List<GuardRotationGroupWithSubgroupsDto>> GetGeneralGroupsWithSubgroupsAsync(CancellationToken ct) =>
        // Nota: los subgrupos se listan sin filtrar por IsActive (a diferencia de antes) para
        // que el frontend pueda mostrar/filtrar inactivos en vez de que queden ocultos sin aviso.
        // Los conteos se calculan como subconsulta (Count(predicate)) en vez de materializar
        // colecciones vía Include, para evitar el cartesian product de incluir varias colecciones
        // hermanas (Employees + Subgroups.Employees) en una sola consulta, que podía producir
        // conteos inconsistentes frente a GetAllAsync.
        await _db.GuardRotationGroups
            .Where(g => g.ParentGroupId == null)
            .OrderBy(g => g.Name)
            .Select(g => new GuardRotationGroupWithSubgroupsDto(
                g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive,
                g.ColorCode, g.GroupLevelType == null ? null : g.GroupLevelType.Name,
                g.Employees.Count(e => e.IsActive),
                g.Subgroups.Count(),
                g.Subgroups.Select(s => new GuardRotationGroupDto(
                    s.GroupId, s.GroupCode, s.Name, s.Description, s.IsActive,
                    s.Employees.Count(e => e.IsActive),
                    g.GroupId, g.Name,
                    s.GroupLevelType == null ? null : s.GroupLevelType.Name, s.ColorCode,
                    0, s.IsSpecial
                )).ToList(),
                g.IsSpecial
            ))
            .ToListAsync(ct);

    public async Task<List<GuardRotationGroupDto>> GetSubgroupsByParentAsync(int parentGroupId, CancellationToken ct) =>
        await _db.GuardRotationGroups
            .Where(g => g.ParentGroupId == parentGroupId)
            .OrderBy(g => g.Name)
            .Select(g => new GuardRotationGroupDto(
                g.GroupId, g.GroupCode, g.Name, g.Description, g.IsActive,
                g.Employees.Count(e => e.IsActive),
                g.ParentGroupId,
                g.ParentGroup == null ? null : g.ParentGroup.Name,
                g.GroupLevelType == null ? null : g.GroupLevelType.Name,
                g.ColorCode,
                g.Subgroups.Count(s => s.IsActive), g.IsSpecial))
            .ToListAsync(ct);
}
