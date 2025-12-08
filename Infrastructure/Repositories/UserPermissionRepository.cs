using Application.DTOs.UserPermissions;
using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de permisos de usuario
/// Accede a las vistas vw_UserRoles y vw_RoleMenuItems
/// </summary>
public class UserPermissionRepository : IUserPermissionRepository
{
    private readonly AppDbContext _context;

    public UserPermissionRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene todos los roles asignados a un usuario desde la vista vw_UserRoles
    /// </summary>
    public async Task<List<UserRoleDto>> GetUserRolesAsync(string userId)
    {
        var roles = await _context.VwUserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => new UserRoleDto
            {
                UserId = ur.UserId,
                Email = ur.Email,
                DisplayName = ur.DisplayName,
                UserType = ur.UserType,
                RoleId = ur.RoleId,
                RoleName = ur.RoleName,
                RoleDescription = ur.RoleDescription,
                AssignedAt = ur.AssignedAt,
                ExpiresAt = ur.ExpiresAt,
                AssignedBy = ur.AssignedBy
            })
            .ToListAsync();

        return roles;
    }

    /// <summary>
    /// Obtiene todos los items de menú asignados a un usuario a través de sus roles
    /// desde la vista vw_RoleMenuItems
    /// </summary>
    public async Task<List<MenuItemDto>> GetUserMenuItemsAsync(string userId)
    {
        // Primero obtenemos los IDs de roles del usuario
        var roleIds = await GetUserRoleIdsAsync(userId);

        if (!roleIds.Any())
        {
            return new List<MenuItemDto>();
        }

        // Luego obtenemos los items de menú de esos roles
        var menuItems = await _context.VwRoleMenuItems
            .Where(rmi => roleIds.Contains(rmi.RoleId))
            .Select(rmi => new MenuItemDto
            {
                RoleId = rmi.RoleId,
                RoleName = rmi.RoleName,
                MenuItemId = rmi.MenuItemId,
                MenuItemName = rmi.MenuItemName,
                Url = rmi.Url,
                Icon = rmi.Icon,
                ParentId = rmi.ParentId,
                Order = rmi.Order,
                IsVisible = rmi.IsVisible,
                RoleSpecificVisibility = rmi.RoleSpecificVisibility
            })
            .ToListAsync();

        return menuItems;
    }

    /// <summary>
    /// Obtiene los IDs de roles asignados a un usuario
    /// </summary>
    public async Task<List<int>> GetUserRoleIdsAsync(string userId)
    {
        var roleIds = await _context.VwUserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync();

        return roleIds;
    }
}
