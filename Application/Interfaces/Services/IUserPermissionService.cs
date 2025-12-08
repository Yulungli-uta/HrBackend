using Application.DTOs.UserPermissions;

namespace Application.Interfaces.Services;

/// <summary>
/// Servicio para gestionar los permisos, roles y menús de usuarios
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Obtiene todos los permisos, roles y menús de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>DTO con roles, permisos (URLs únicas) y menús</returns>
    Task<UserPermissionsDto> GetUserPermissionsAsync(string userId);

    /// <summary>
    /// Obtiene solo los roles de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de roles</returns>
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Obtiene solo los items de menú de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de items de menú</returns>
    Task<List<MenuItemDto>> GetUserMenuItemsAsync(string userId);

    /// <summary>
    /// Obtiene solo las URLs únicas (permisos) de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de URLs únicas</returns>
    Task<List<string>> GetUserPermissionsUrlsAsync(string userId);
}
