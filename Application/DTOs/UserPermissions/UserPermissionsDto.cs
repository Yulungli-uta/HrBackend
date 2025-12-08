namespace Application.DTOs.UserPermissions;

/// <summary>
/// DTO que contiene todos los permisos, roles y menús de un usuario
/// </summary>
public class UserPermissionsDto
{
    /// <summary>
    /// Lista de roles asignados al usuario
    /// </summary>
    public List<UserRoleDto> Roles { get; set; } = new();

    /// <summary>
    /// Lista de URLs únicas a las que el usuario tiene acceso (permisos)
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Lista de items de menú asignados al usuario
    /// </summary>
    public List<MenuItemDto> MenuItems { get; set; } = new();
}
