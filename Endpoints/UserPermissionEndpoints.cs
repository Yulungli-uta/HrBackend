using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace WsUtaSystem.Endpoints;

/// <summary>
/// Endpoints para gestionar permisos, roles y menús de usuarios
/// </summary>
public static class UserPermissionEndpoints
{
    public static void MapUserPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("User Permissions");
            //.RequireAuthorization(); // Descomentar cuando esté listo para producción

        // GET /api/users/{userId}/permissions
        // Obtiene todos los permisos, roles y menús de un usuario
        group.MapGet("/{userId}/permissions", async (
            [FromRoute] string userId,
            [FromServices] IUserPermissionService permissionService) =>
        {
            try
            {
                var permissions = await permissionService.GetUserPermissionsAsync(userId);
                return Results.Ok(permissions);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error al obtener permisos del usuario"
                );
            }
        })
        .WithName("GetUserPermissions")
        .WithDescription("Obtiene todos los permisos, roles y menús de un usuario")
        .Produces<Application.DTOs.UserPermissions.UserPermissionsDto>(200)
        .Produces(500);

        // GET /api/users/{userId}/roles
        // Obtiene solo los roles de un usuario
        group.MapGet("/{userId}/roles", async (
            [FromRoute] string userId,
            [FromServices] IUserPermissionService permissionService) =>
        {
            try
            {
                var roles = await permissionService.GetUserRolesAsync(userId);
                return Results.Ok(roles);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error al obtener roles del usuario"
                );
            }
        })
        .WithName("GetUserRoles")
        .WithDescription("Obtiene solo los roles asignados a un usuario")
        .Produces<List<Application.DTOs.UserPermissions.UserRoleDto>>(200)
        .Produces(500);

        // GET /api/users/{userId}/menu-items
        // Obtiene solo los items de menú de un usuario
        group.MapGet("/{userId}/menu-items", async (
            [FromRoute] string userId,
            [FromServices] IUserPermissionService permissionService) =>
        {
            try
            {
                var menuItems = await permissionService.GetUserMenuItemsAsync(userId);
                return Results.Ok(menuItems);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error al obtener menús del usuario"
                );
            }
        })
        .WithName("GetUserMenuItems")
        .WithDescription("Obtiene solo los items de menú asignados a un usuario")
        .Produces<List<Application.DTOs.UserPermissions.MenuItemDto>>(200)
        .Produces(500);

        // GET /api/users/{userId}/permissions-urls
        // Obtiene solo las URLs únicas (permisos) de un usuario
        group.MapGet("/{userId}/permissions-urls", async (
            [FromRoute] string userId,
            [FromServices] IUserPermissionService permissionService) =>
        {
            try
            {
                var urls = await permissionService.GetUserPermissionsUrlsAsync(userId);
                return Results.Ok(urls);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error al obtener URLs de permisos del usuario"
                );
            }
        })
        .WithName("GetUserPermissionsUrls")
        .WithDescription("Obtiene solo las URLs únicas (permisos) de un usuario")
        .Produces<List<string>>(200)
        .Produces(500);
    }
}
