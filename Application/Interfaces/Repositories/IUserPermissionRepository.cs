using Application.DTOs.UserPermissions;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repositorio para acceder a los permisos, roles y menús de usuarios
/// </summary>
public interface IUserPermissionRepository
{
    /// <summary>
    /// Obtiene todos los roles asignados a un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de roles con toda la información disponible</returns>
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Obtiene todos los items de menú asignados a un usuario a través de sus roles
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de items de menú con toda la información disponible</returns>
    Task<List<MenuItemDto>> GetUserMenuItemsAsync(string userId);

    /// <summary>
    /// Obtiene los IDs de roles asignados a un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de IDs de roles</returns>
    Task<List<int>> GetUserRoleIdsAsync(string userId);
}
