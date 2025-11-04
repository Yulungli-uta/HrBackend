using WsUtaSystem.Infrastructure.Services;

namespace WsUtaSystem.Middleware;

/// <summary>
/// Middleware que valida tokens JWT en todas las peticiones
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly bool _enableLogging;
    private readonly HashSet<string> _publicPaths;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _enableLogging = bool.TryParse(configuration["AuthService:EnableLogging"], out var logging) ? logging : true;
        
        // Cargar rutas públicas desde configuración o usar valores por defecto
        var publicPathsConfig = configuration.GetSection("AuthService:PublicPaths").Get<string[]>();
        _publicPaths = new HashSet<string>(
            publicPathsConfig ?? new[] { "/health", "/swagger", "/api/v1/rh/public" },
            StringComparer.OrdinalIgnoreCase
        );
    }

    public async Task InvokeAsync(HttpContext context, ITokenValidationService tokenValidationService)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Permitir endpoints públicos
        if (IsPublicEndpoint(path))
        {
            if (_enableLogging)
                _logger.LogDebug("Public endpoint accessed: {Path}", path);
            
            await _next(context);
            return;
        }

        // Extraer token del header Authorization
        var authHeader = context.Request.Headers["Authorization"].ToString();
        var token = ExtractToken(authHeader);

        if (string.IsNullOrEmpty(token))
        {
            if (_enableLogging)
                _logger.LogWarning("Request to {Path} rejected: No token provided", path);
            
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Token no proporcionado",
                message = "Se requiere autenticación para acceder a este recurso",
                statusCode = 401
            });
            return;
        }

        // Validar token contra el servicio de autenticación
        var validationResult = await tokenValidationService.ValidateTokenAsync(token);

        if (!validationResult.IsValid)
        {
            if (_enableLogging)
                _logger.LogWarning("Request to {Path} rejected: Invalid token - {Message}", path, validationResult.Message);
            
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Token inválido o expirado",
                message = validationResult.Message ?? "El token proporcionado no es válido",
                statusCode = 401
            });
            return;
        }

        // Agregar información del usuario al contexto para uso en controladores
        context.Items["UserId"] = validationResult.UserId;
        context.Items["UserEmail"] = validationResult.Email;
        context.Items["UserRoles"] = validationResult.Roles;

        if (_enableLogging)
            _logger.LogInformation("Request to {Path} authorized for user {Email}", path, validationResult.Email);

        await _next(context);
    }

    /// <summary>
    /// Verifica si el endpoint es público y no requiere autenticación
    /// </summary>
    private bool IsPublicEndpoint(string path)
    {
        // Verificar coincidencias exactas o prefijos
        return _publicPaths.Any(publicPath =>
            path.Equals(publicPath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Extrae el token del header Authorization
    /// </summary>
    private string? ExtractToken(string authHeader)
    {
        if (string.IsNullOrEmpty(authHeader))
            return null;

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            if (_enableLogging)
                _logger.LogWarning("Invalid Authorization header format: {Header}", authHeader);
            
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }
}

/// <summary>
/// Clase de extensión para facilitar el acceso a la información del usuario autenticado
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Obtiene el ID del usuario autenticado desde el contexto
    /// </summary>
    public static string? GetUserId(this HttpContext context)
    {
        return context.Items["UserId"]?.ToString();
    }

    /// <summary>
    /// Obtiene el email del usuario autenticado desde el contexto
    /// </summary>
    public static string? GetUserEmail(this HttpContext context)
    {
        return context.Items["UserEmail"]?.ToString();
    }

    /// <summary>
    /// Obtiene los roles del usuario autenticado desde el contexto
    /// </summary>
    public static List<string> GetUserRoles(this HttpContext context)
    {
        return context.Items["UserRoles"] as List<string> ?? new List<string>();
    }

    /// <summary>
    /// Verifica si el usuario autenticado tiene un rol específico
    /// </summary>
    public static bool HasRole(this HttpContext context, string role)
    {
        var roles = context.GetUserRoles();
        return roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
