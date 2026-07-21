namespace WsUtaSystem.Application.Common.Interfaces;

/// <summary>
/// Punto único para validar acceso por departamento a un registro individual, sobre
/// <see cref="IUserAccessScopeService"/>. Reemplaza llamar <c>EnsureDepartmentAllowedAsync</c>
/// a mano en cada controller (lo que causó huecos de cobertura en Contracts/PersonnelActions:
/// Create/GetPaged lo tenían, GetById/Update/Delete/Approve no).
/// Lanza <see cref="UnauthorizedAccessException"/> si no tiene acceso.
/// </summary>
public interface IRecordAccessGuard
{
    /// <summary>Valida acceso cuando ya se conoce el DepartmentId del registro (ej. Contracts.DepartmentID).</summary>
    Task EnsureDepartmentAsync(int departmentId, string moduleCode, CancellationToken ct = default);

    /// <summary>
    /// Valida acceso resolviendo el departamento a partir del empleado dueño del registro
    /// (ej. Vacations.EmployeeId, Permissions.EmployeeId). Acceder a tu propio registro
    /// siempre está permitido, sin necesidad de scope de departamento.
    /// </summary>
    Task EnsureEmployeeRecordAsync(int recordOwnerEmployeeId, string moduleCode, CancellationToken ct = default);
}
