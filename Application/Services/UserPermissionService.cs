using Application.DTOs.UserPermissions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services;

/// <summary>
/// Implementación del servicio de permisos de usuario
/// Contiene la lógica de negocio para gestionar permisos, roles y menús
/// </summary>
public class UserPermissionService : IUserPermissionService
{
    private readonly IUserPermissionRepository _repository;

    public UserPermissionService(IUserPermissionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Obtiene todos los permisos, roles y menús de un usuario
    /// </summary>
    public async Task<UserPermissionsDto> GetUserPermissionsAsync(string userId)
    {
        // Obtener roles y menús en paralelo para mejor performance
        var rolesTask = _repository.GetUserRolesAsync(userId);
        var menuItemsTask = _repository.GetUserMenuItemsAsync(userId);

        await Task.WhenAll(rolesTask, menuItemsTask);

        var roles = await rolesTask;
        var menuItems = await menuItemsTask;

        // Extraer URLs únicas de los items de menú (permisos)
        var permissions = menuItems
            .Where(mi => !string.IsNullOrWhiteSpace(mi.Url))
            .Select(mi => mi.Url!)
            .Distinct()
            .OrderBy(url => url)
            .ToList();

        return new UserPermissionsDto
        {
            Roles = roles,
            Permissions = permissions,
            MenuItems = menuItems
        };
    }

    /// <summary>
    /// Obtiene solo los roles de un usuario
    /// </summary>
    public async Task<List<UserRoleDto>> GetUserRolesAsync(string userId)
    {
        return await _repository.GetUserRolesAsync(userId);
    }

    /// <summary>
    /// Obtiene solo los items de menú de un usuario
    /// </summary>
    public async Task<List<MenuItemDto>> GetUserMenuItemsAsync(string userId)
    {
        return await _repository.GetUserMenuItemsAsync(userId);
    }

    /// <summary>
    /// Obtiene solo las URLs únicas (permisos) de un usuario
    /// </summary>
    public async Task<List<string>> GetUserPermissionsUrlsAsync(string userId)
    {
        var menuItems = await _repository.GetUserMenuItemsAsync(userId);

        var permissions = menuItems
            .Where(mi => !string.IsNullOrWhiteSpace(mi.Url))
            .Select(mi => mi.Url!)
            .Distinct()
            .OrderBy(url => url)
            .ToList();

        return permissions;
    }
}
