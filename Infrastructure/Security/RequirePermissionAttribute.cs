using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Infrastructure.Security;

/// <summary>
/// Exige que el usuario autenticado tenga el permiso de acción indicado (código
/// "MODULO.ACCION", ver catálogo en RepositoryUta). No depende de <c>[Authorize]</c>/
/// <c>UseAuthorization()</c> — HrBackend no tiene ese pipeline conectado hoy (el único gate
/// real es <see cref="WsUtaSystem.Middleware.JwtAuthenticationMiddleware"/>, que solo valida
/// autenticación). Se implementa como filtro de acción independiente para poder aplicarse
/// endpoint por endpoint sin activar de golpe los atributos <c>[Authorize]</c> ya presentes
/// (y hoy inertes) en otros ~110 controllers del proyecto.
///
/// Modo sombra (<c>Authorization:ShadowMode</c>, default true): registra en log quién
/// habría sido bloqueado, pero deja pasar la request. Poner en <c>false</c> solo cuando el
/// catálogo de permisos y la matriz rol→permiso en RepositoryUta ya estén poblados y
/// validados — mientras estén vacíos, este atributo en modo estricto bloquearía a todos.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _permissionCode;

    public RequirePermissionAttribute(string permissionCode)
    {
        _permissionCode = permissionCode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var permissionService = services.GetRequiredService<IUserActionPermissionService>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("RequirePermission");

        // Default true (modo sombra) si la clave no está configurada explícitamente.
        var shadowMode = configuration["Authorization:ShadowMode"] is null
            || (bool.TryParse(configuration["Authorization:ShadowMode"], out var sm) && sm);

        var roles = context.HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var hasPermission = await permissionService.HasPermissionAsync(roles, _permissionCode, context.HttpContext.RequestAborted);

        if (!hasPermission)
        {
            var userEmail = context.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "desconocido";

            if (shadowMode)
            {
                logger.LogWarning(
                    "[RequirePermission:SHADOW] {Email} con roles [{Roles}] habría sido bloqueado por falta de {Permission} en {Path}",
                    userEmail, string.Join(',', roles), _permissionCode, context.HttpContext.Request.Path);
            }
            else
            {
                context.Result = new ObjectResult(new
                {
                    status = "error",
                    error = new
                    {
                        code = "FORBIDDEN",
                        message = "No tiene permisos para realizar esta acción.",
                        details = new { requiredPermission = _permissionCode },
                        traceId = context.HttpContext.TraceIdentifier
                    }
                })
                { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        }

        await next();
    }
}
