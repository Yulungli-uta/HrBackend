using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.UserAccessScope;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Gestiona las asignaciones de alcance (departamento/facultad) por usuario y por módulo
/// (Contratos, Acciones de Personal, ...). Pieza central reusable: cualquier módulo nuevo
/// solo necesita llamar <see cref="GetAllowedDepartmentIdsAsync"/> o
/// <see cref="EnsureDepartmentAllowedAsync"/> con su propio código de módulo.
/// </summary>
public class UserAccessScopeService : IUserAccessScopeService
{
    private const string ModuleCategory = "ACCESS_MODULE_TYPE";
    private const string ScopeGlobal = "GLOBAL";
    private const string ScopeDepartmentTree = "DEPARTMENT_TREE";

    private readonly IUserAccessScopeRepository _repository;
    private readonly AppDbContext _db;

    public UserAccessScopeService(IUserAccessScopeRepository repository, AppDbContext db)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<List<UserAccessScopeDto>> ListAsync(CancellationToken ct = default)
    {
        var items = await _db.UserAccessScopes
            .AsNoTracking()
            .Include(s => s.ModuleType)
            .Include(s => s.ScopeType)
            .Include(s => s.Department)
            .OrderByDescending(s => s.AssignedAt)
            .ToListAsync(ct);

        var employeeIds = items.Select(s => s.EmployeeId).Distinct().ToList();
        var employeeNames = await _db.Set<WsUtaSystem.Models.Views.VwEmployeeDetails>()
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.EmployeeID))
            .ToDictionaryAsync(e => e.EmployeeID, e => (e.LastName + " " + e.FirstName, e.Email), ct);

        return items.Select(s => ToDto(s, employeeNames)).ToList();
    }

    public async Task<UserAccessScopeDto> CreateAsync(UserAccessScopeCreateDto dto, string changedBy, CancellationToken ct = default)
    {
        ValidateScopeDepartmentConsistency(dto.ScopeTypeId, dto.DepartmentId);

        var now = DateTime.Now;
        var existing = await _db.UserAccessScopes
            .FirstOrDefaultAsync(s => s.EmployeeId == dto.EmployeeId
                                   && s.ModuleTypeId == dto.ModuleTypeId
                                   && s.DepartmentId == dto.DepartmentId, ct);

        if (existing is not null)
        {
            var isActive = existing.IsActive && (existing.ExpiresAt == null || existing.ExpiresAt > now);
            if (isActive)
                throw new InvalidOperationException("El empleado ya tiene un acceso activo para ese módulo y departamento.");

            // Reactivar fila inactiva en vez de duplicar.
            existing.IsActive = true;
            existing.ScopeTypeId = dto.ScopeTypeId;
            existing.AssignedAt = now;
            existing.ExpiresAt = dto.ExpiresAt;
            existing.AssignedBy = changedBy;
            existing.Reason = dto.Reason;

            await _db.SaveChangesAsync(ct);
            await AddHistoryAsync(existing.Id, dto.EmployeeId, dto.ModuleTypeId, "Assigned",
                null, null, dto.ScopeTypeId, dto.DepartmentId, changedBy, dto.Reason, ct);

            return await ToDtoWithNamesAsync(existing, ct);
        }

        var entity = new UserAccessScope
        {
            EmployeeId = dto.EmployeeId,
            ModuleTypeId = dto.ModuleTypeId,
            ScopeTypeId = dto.ScopeTypeId,
            DepartmentId = dto.DepartmentId,
            IsActive = true,
            AssignedAt = now,
            ExpiresAt = dto.ExpiresAt,
            AssignedBy = changedBy,
            Reason = dto.Reason,
        };

        _db.UserAccessScopes.Add(entity);
        await _db.SaveChangesAsync(ct);

        await AddHistoryAsync(entity.Id, dto.EmployeeId, dto.ModuleTypeId, "Assigned",
            null, null, dto.ScopeTypeId, dto.DepartmentId, changedBy, dto.Reason, ct);

        return await ToDtoWithNamesAsync(entity, ct);
    }

    public async Task<UserAccessScopeDto?> UpdateAsync(int id, UserAccessScopeUpdateDto dto, string changedBy, CancellationToken ct = default)
    {
        ValidateScopeDepartmentConsistency(dto.ScopeTypeId, dto.DepartmentId);

        var current = await _db.UserAccessScopes.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (current is null) return null;

        var previousScopeTypeId = current.ScopeTypeId;
        var previousDepartmentId = current.DepartmentId;

        current.ScopeTypeId = dto.ScopeTypeId;
        current.DepartmentId = dto.DepartmentId;
        current.ExpiresAt = dto.ExpiresAt;
        current.Reason = dto.Reason;

        await _db.SaveChangesAsync(ct);

        await AddHistoryAsync(current.Id, current.EmployeeId, current.ModuleTypeId, "Modified",
            previousScopeTypeId, previousDepartmentId, dto.ScopeTypeId, dto.DepartmentId, changedBy, dto.Reason, ct);

        return await ToDtoWithNamesAsync(current, ct);
    }

    public async Task<bool> DeleteAsync(int id, string changedBy, CancellationToken ct = default)
    {
        var current = await _db.UserAccessScopes.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (current is null) return false;

        current.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await AddHistoryAsync(current.Id, current.EmployeeId, current.ModuleTypeId, "Removed",
            current.ScopeTypeId, current.DepartmentId, null, null, changedBy, null, ct);

        return true;
    }

    public async Task<List<UserAccessScopeHistoryDto>> GetHistoryAsync(int employeeId, CancellationToken ct = default)
    {
        var history = await _repository.GetHistoryByEmployeeAsync(employeeId, ct);
        return history.Select(h => new UserAccessScopeHistoryDto
        {
            Id = h.Id,
            EmployeeId = h.EmployeeId,
            ModuleTypeId = h.ModuleTypeId,
            ChangeType = h.ChangeType,
            PreviousScopeTypeId = h.PreviousScopeTypeId,
            PreviousDepartmentId = h.PreviousDepartmentId,
            NewScopeTypeId = h.NewScopeTypeId,
            NewDepartmentId = h.NewDepartmentId,
            ChangedBy = h.ChangedBy,
            ChangeReason = h.ChangeReason,
            ChangeDateTime = h.ChangeDateTime,
        }).ToList();
    }

    public async Task<List<int>?> GetAllowedDepartmentIdsAsync(int employeeId, string moduleCode, CancellationToken ct = default)
    {
        var scopes = await _repository.GetActiveByEmployeeAndModuleAsync(employeeId, moduleCode, ct);

        // Sin scopes asignados aún: sin restricción (comportamiento transitorio, ver historial de diseño).
        if (scopes.Count == 0) return null;

        // Cualquier fila GLOBAL anula toda restricción.
        if (scopes.Any(s => s.ScopeType?.Name == ScopeGlobal)) return null;

        var allowed = new List<int>();
        foreach (var scope in scopes)
        {
            if (scope.DepartmentId is null) continue;

            if (scope.ScopeType?.Name == ScopeDepartmentTree)
                allowed.AddRange(await _repository.GetDepartmentTreeIdsAsync(scope.DepartmentId.Value, ct));
            else
                allowed.Add(scope.DepartmentId.Value);
        }

        return allowed.Distinct().ToList();
    }

    public async Task EnsureDepartmentAllowedAsync(int employeeId, string moduleCode, int departmentId, CancellationToken ct = default)
    {
        var allowed = await GetAllowedDepartmentIdsAsync(employeeId, moduleCode, ct);

        // null = sin restricción
        if (allowed is null) return;

        if (!allowed.Contains(departmentId))
            throw new UnauthorizedAccessException(
                "No tiene permiso para crear o gestionar registros en este departamento.");
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private static void ValidateScopeDepartmentConsistency(int scopeTypeId, int? departmentId)
    {
        // No se valida el nombre aquí (requeriría consulta a BD); se hace en ToDto/EnsureDepartmentAllowedAsync.
        // Validación mínima: si no hay departamento, el llamador debe haber elegido GLOBAL en la UI.
    }

    private async Task AddHistoryAsync(
        int scopeId, int employeeId, int moduleTypeId, string changeType,
        int? previousScopeTypeId, int? previousDepartmentId,
        int? newScopeTypeId, int? newDepartmentId,
        string changedBy, string? reason, CancellationToken ct)
    {
        await _repository.AddHistoryAsync(new UserAccessScopeHistory
        {
            ScopeId = scopeId,
            EmployeeId = employeeId,
            ModuleTypeId = moduleTypeId,
            ChangeType = changeType,
            PreviousScopeTypeId = previousScopeTypeId,
            PreviousDepartmentId = previousDepartmentId,
            NewScopeTypeId = newScopeTypeId,
            NewDepartmentId = newDepartmentId,
            ChangedBy = changedBy,
            ChangeReason = reason,
            ChangeDateTime = DateTime.Now,
        }, ct);
    }

    private static UserAccessScopeDto ToDto(UserAccessScope s, Dictionary<int, (string Name, string? Email)>? employeeNames = null)
    {
        (string Name, string? Email) employee = default;
        employeeNames?.TryGetValue(s.EmployeeId, out employee);

        return new UserAccessScopeDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            EmployeeName = employee.Name,
            EmployeeEmail = employee.Email,
            ModuleTypeId = s.ModuleTypeId,
            ModuleTypeName = s.ModuleType?.Name,
            ScopeTypeId = s.ScopeTypeId,
            ScopeTypeName = s.ScopeType?.Name,
            DepartmentId = s.DepartmentId,
            DepartmentName = s.Department?.Name,
            IsActive = s.IsActive,
            AssignedAt = s.AssignedAt,
            ExpiresAt = s.ExpiresAt,
            AssignedBy = s.AssignedBy,
            Reason = s.Reason,
        };
    }

    private async Task<UserAccessScopeDto> ToDtoWithNamesAsync(UserAccessScope s, CancellationToken ct)
    {
        var moduleName = await _db.RefTypes.AsNoTracking()
            .Where(r => r.TypeId == s.ModuleTypeId).Select(r => r.Name).FirstOrDefaultAsync(ct);
        var scopeName = await _db.RefTypes.AsNoTracking()
            .Where(r => r.TypeId == s.ScopeTypeId).Select(r => r.Name).FirstOrDefaultAsync(ct);
        var deptName = s.DepartmentId.HasValue
            ? await _db.Departments.AsNoTracking()
                .Where(d => d.DepartmentId == s.DepartmentId.Value).Select(d => d.Name).FirstOrDefaultAsync(ct)
            : null;
        var employee = await _db.Set<WsUtaSystem.Models.Views.VwEmployeeDetails>()
            .AsNoTracking()
            .Where(e => e.EmployeeID == s.EmployeeId)
            .Select(e => new { e.FirstName, e.LastName, e.Email })
            .FirstOrDefaultAsync(ct);

        return new UserAccessScopeDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            EmployeeName = employee is null ? null : $"{employee.LastName} {employee.FirstName}".Trim(),
            EmployeeEmail = employee?.Email,
            ModuleTypeId = s.ModuleTypeId,
            ModuleTypeName = moduleName,
            ScopeTypeId = s.ScopeTypeId,
            ScopeTypeName = scopeName,
            DepartmentId = s.DepartmentId,
            DepartmentName = deptName,
            IsActive = s.IsActive,
            AssignedAt = s.AssignedAt,
            ExpiresAt = s.ExpiresAt,
            AssignedBy = s.AssignedBy,
            Reason = s.Reason,
        };
    }
}
