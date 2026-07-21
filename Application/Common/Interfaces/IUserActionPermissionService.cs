namespace WsUtaSystem.Application.Common.Interfaces;

/// <summary>
/// Resuelve si un conjunto de roles (los del usuario autenticado) tiene un permiso de
/// acción (código "MODULO.ACCION") según la matriz RolePermission de RepositoryUta.
/// </summary>
public interface IUserActionPermissionService
{
    Task<bool> HasPermissionAsync(IEnumerable<string> roles, string permissionCode, CancellationToken ct = default);
}
