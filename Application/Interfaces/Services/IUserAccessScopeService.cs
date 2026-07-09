using WsUtaSystem.Application.DTOs.UserAccessScope;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IUserAccessScopeService
{
    Task<List<UserAccessScopeDto>> ListAsync(CancellationToken ct = default);

    Task<UserAccessScopeDto> CreateAsync(UserAccessScopeCreateDto dto, string changedBy, CancellationToken ct = default);

    Task<UserAccessScopeDto?> UpdateAsync(int id, UserAccessScopeUpdateDto dto, string changedBy, CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, string changedBy, CancellationToken ct = default);

    Task<List<UserAccessScopeHistoryDto>> GetHistoryAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Resuelve los departamentos permitidos para un empleado en un módulo.
    /// Retorna null = sin restricción (GLOBAL o sin scopes asignados aún).
    /// Retorna lista vacía = no tiene acceso a ningún departamento.
    /// </summary>
    Task<List<int>?> GetAllowedDepartmentIdsAsync(int employeeId, string moduleCode, CancellationToken ct = default);

    /// <summary>
    /// Valida que el departamento indicado esté dentro del alcance permitido del empleado
    /// para el módulo dado. Lanza <see cref="UnauthorizedAccessException"/> si no lo está.
    /// </summary>
    Task EnsureDepartmentAllowedAsync(int employeeId, string moduleCode, int departmentId, CancellationToken ct = default);
}
