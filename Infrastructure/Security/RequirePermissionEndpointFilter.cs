using System.Security.Claims;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Infrastructure.Security;

/// <summary>
/// Equivalente a <see cref="RequirePermissionAttribute"/> pero para Minimal API
/// (<see cref="Endpoints.ReportEndpoints"/> usa <c>MapGroup</c>/<c>MapPost</c>, no controllers,
/// así que el filtro de acción MVC no aplica). Misma lógica de modo sombra.
/// </summary>
public sealed class RequirePermissionEndpointFilter : IEndpointFilter
{
    private readonly string _permissionCode;

    public RequirePermissionEndpointFilter(string permissionCode)
    {
        _permissionCode = permissionCode;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var services = httpContext.RequestServices;
        var permissionService = services.GetRequiredService<IUserActionPermissionService>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("RequirePermission");

        var shadowMode = configuration["Authorization:ShadowMode"] is null
            || (bool.TryParse(configuration["Authorization:ShadowMode"], out var sm) && sm);

        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var hasPermission = await permissionService.HasPermissionAsync(roles, _permissionCode, httpContext.RequestAborted);

        if (!hasPermission)
        {
            var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "desconocido";

            if (shadowMode)
            {
                logger.LogWarning(
                    "[RequirePermission:SHADOW] {Email} con roles [{Roles}] habría sido bloqueado por falta de {Permission} en {Path}",
                    userEmail, string.Join(',', roles), _permissionCode, httpContext.Request.Path);
            }
            else
            {
                return Results.Json(new
                {
                    status = "error",
                    error = new
                    {
                        code = "FORBIDDEN",
                        message = "No tiene permisos para realizar esta acción.",
                        details = new { requiredPermission = _permissionCode },
                        traceId = httpContext.TraceIdentifier
                    }
                }, statusCode: StatusCodes.Status403Forbidden);
            }
        }

        return await next(context);
    }
}

public static class RequirePermissionEndpointFilterExtensions
{
    /// <summary>Aplica el gate de permiso de acción a un endpoint o grupo de Minimal API.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissionCode)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(new RequirePermissionEndpointFilter(permissionCode));
        return builder;
    }
}
