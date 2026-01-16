using System.Security.Claims;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Services;
using WsUtaSystem.Infrastructure.Services;
using WsUtaSystem.Models;

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
    private readonly IvwEmployeeDetailsService _employeeDetailsService;

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

    public async Task InvokeAsync(HttpContext context, ITokenValidationService tokenValidationService, IvwEmployeeDetailsService employeeDetailsService)
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

        // 1) Mantener Items (compatibilidad con lo que ya tienes)
        context.Items["UserId"] = validationResult.UserId;
        context.Items["UserEmail"] = validationResult.Email;
        context.Items["UserRoles"] = validationResult.Roles;
        

        // ✅ Resolver EmployeeId (fallback con tu vista)
        int? employeeId = null;
        employeeId = TryGetEmployeeId(validationResult); // intenta desde result (si existe)

        if (!string.IsNullOrWhiteSpace(validationResult.Email))
        {
            _logger.LogDebug("Resolving EmployeeID from view by email: {Email}", validationResult.Email);

            var emp = await employeeDetailsService.GetByEmailAsync(validationResult.Email, context.RequestAborted);

            if (_enableLogging)
                _logger.LogInformation("Fetched employee details: {@Employee}", emp);

            // ✅ Tu view usa EmployeeID (así se llama la prop)
            employeeId = emp?.EmployeeID;
        }

        // Guarda SIEMPRE (para CurrentUserService fallback)
        context.Items["EmployeeId"] = employeeId;

        // 2) ✅ Crear ClaimsPrincipal para que Controller/User/ICurrentUserService funcionen
        var principal = BuildPrincipal(
            email: validationResult.Email,
            userId: validationResult.UserId,
            roles: validationResult.Roles,
            employeeId: employeeId
        );

        

        context.User = principal;

        if (_enableLogging)
        {
            _logger.LogInformation("Request to {Path} authorized for user {Email}", path, validationResult.Email);
            _logger.LogDebug("User injected into HttpContext.User. IsAuth={IsAuth}", context.User?.Identity?.IsAuthenticated);
        }

        await _next(context);
    }

    private int? TryGetEmployeeId(dynamic validationResult)
    {
        // Si tu ValidateTokenAsync ya retorna EmployeeId, úsalo.
        // Si no existe, devuelve null (y luego lo podrás mapear en el auth-service para incluirlo).
        try
        {
            //_logger.LogInformation("Attempting to extract EmployeeId from validation result. userid:{UserId}",
            //    validationResult.EmployeeId);
            // Ej: validationResult.EmployeeId            
            var value = validationResult.EmployeeId;
            if (value is null) return null;

            if (value is int i) return i;

            if (int.TryParse(value.ToString(), out int parsed))
                return parsed;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static ClaimsPrincipal BuildPrincipal(string? email, string? userId, IEnumerable<string>? roles, int? employeeId)
    {
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim(ClaimTypes.Name, email)); // útil para logs/Identity.Name
        }

        // NameIdentifier es estándar, muchos sistemas lo usan
        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            claims.Add(new Claim("userId", userId));
            claims.Add(new Claim("sub", userId));
        }

        // Claim esperado por tu CurrentUserService
        if (employeeId.HasValue)
        {
            claims.Add(new Claim("employeeId", employeeId.Value.ToString()));
        }

        // Roles
        if (roles != null)
        {
            foreach (var r in roles.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
                claims.Add(new Claim("role", r));
            }
        }

        // ✅ authenticationType NO vacío => IsAuthenticated = true
        var identity = new ClaimsIdentity(claims, authenticationType: "JwtCustom");
        return new ClaimsPrincipal(identity);
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
    public static string? GetUserId(this HttpContext context) =>
        context.Items["UserId"]?.ToString();

    public static string? GetUserEmail(this HttpContext context) =>
        context.Items["UserEmail"]?.ToString();

    public static List<string> GetUserRoles(this HttpContext context) =>
        context.Items["UserRoles"] as List<string> ?? new List<string>();

    public static bool HasRole(this HttpContext context, string role)
    {
        var roles = context.GetUserRoles();
        return roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
