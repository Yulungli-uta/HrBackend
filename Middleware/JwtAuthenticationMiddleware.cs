using System.Diagnostics;
using System.Security.Claims;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Services;

namespace WsUtaSystem.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly bool _enableLogging;
    private readonly HashSet<string> _publicPaths;

    private static readonly string[] SkipEmployeeLookupMarkers =
    {
        "/vw/employeedetails",
        "/api/v1/rh/vw/employeedetails"
    };

    private const string SkipLookupFlagKey = "__skip_employee_lookup";

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        _enableLogging = bool.TryParse(configuration["AuthService:EnableLogging"], out var logging) ? logging : true;

        var publicPathsConfig = configuration.GetSection("AuthService:PublicPaths").Get<string[]>();
        _publicPaths = new HashSet<string>(
            publicPathsConfig ?? new[] { "/health", "/swagger", "/api/v1/rh/public" },
            StringComparer.OrdinalIgnoreCase
        );

        //_logger.LogInformation("[AUTH-MW] Middleware inicializado. Rutas públicas: {Paths}", string.Join(" | ", _publicPaths));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        var traceId = context.TraceIdentifier;

        // Log inicial crítico para confirmar que el middleware está vivo
        //_logger.LogInformation("[AUTH-MW] Request recibida: {Method} {Path} (Trace: {TraceId})", method, path, traceId);

        var swTotal = Stopwatch.StartNew();

        try
        {
            // 1. Bypass para OPTIONS (CORS)
            if (HttpMethods.IsOptions(method))
            {
                await _next(context);
                return;
            }

            // 2. Bypass para rutas públicas
            if (TryMatchPublicEndpoint(path, out _))
            {
                await _next(context);
                return;
            }

            // 3. Extracción de Token
            var authHeader = context.Request.Headers["Authorization"].ToString();
            var token = ExtractToken(authHeader);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("[AUTH-MW] No se proporcionó token para: {Path}", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "No autorizado", message = "Token requerido" });
                return;
            }

            // 4. Resolución de Servicios (IMPORTANTE: Esto evita el Deadlock)
            // Resolvemos aquí dentro para que si hay una circularidad, podamos loguear el error exacto
            var tokenValidationService = context.RequestServices.GetRequiredService<ITokenValidationService>();
            var employeeDetailsService = context.RequestServices.GetRequiredService<IvwEmployeeDetailsService>();

            // 5. Validación de Token
            var validationResult = await tokenValidationService.ValidateTokenAsync(token);

            if (!validationResult.IsValid)
            {
                //_logger.LogWarning("[AUTH-MW] Token inválido: {Msg}", validationResult.Message);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Token inválido", message = validationResult.Message });
                return;
            }

            // 6. Inyección de Claims y datos básicos en el Contexto
            context.Items["UserId"] = validationResult.UserId;
            context.Items["UserEmail"] = validationResult.Email;
            context.Items["UserRoles"] = validationResult.Roles;

            // 7. Búsqueda de EmployeeId (con guardián anti-recursión)
            int? employeeId = null;
            var skipLookup = ShouldSkipEmployeeLookup(path) || (context.Items.TryGetValue(SkipLookupFlagKey, out var f) && f is true);

            if (!skipLookup && !string.IsNullOrWhiteSpace(validationResult.Email))
            {
                context.Items[SkipLookupFlagKey] = true; // Marcamos para evitar re-entrada
                try
                {
                    // Timeout de seguridad de 5 segundos para la DB
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                    cts.CancelAfter(TimeSpan.FromSeconds(5));

                    var emp = await employeeDetailsService.GetByEmailAsync(validationResult.Email, cts.Token);
                    employeeId = emp?.EmployeeID;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTH-MW] Error buscando empleado para {Email}", validationResult.Email);
                }
            }

            context.Items["EmployeeId"] = employeeId;

            // 8. Construir el Principal para el sistema de seguridad de .NET
            context.User = BuildPrincipal(validationResult.Email, validationResult.UserId, validationResult.Roles, employeeId);

            // 9. Continuar la cadena
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH-MW] Error crítico en el pipeline de autenticación");
            throw; // Re-lanzar para que el middleware de excepciones lo capture
        }
        finally
        {
            swTotal.Stop();
            if (_enableLogging)
                _logger.LogInformation("[AUTH-MW] Fin de procesamiento. Path: {Path} Status: {Status} Time: {Elapsed}ms",
                    path, context.Response.StatusCode, swTotal.ElapsedMilliseconds);
        }
    }

    private bool ShouldSkipEmployeeLookup(string path)
    {
        return SkipEmployeeLookupMarkers.Any(m => path.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryMatchPublicEndpoint(string path, out string matched)
    {
        foreach (var publicPath in _publicPaths)
        {
            if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
            {
                matched = publicPath;
                return true;
            }
        }
        matched = string.Empty;
        return false;
    }

    private static ClaimsPrincipal BuildPrincipal(string? email, string? userId, IEnumerable<string>? roles, int? employeeId)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(email)) claims.Add(new Claim(ClaimTypes.Email, email));
        if (!string.IsNullOrWhiteSpace(userId)) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        if (employeeId.HasValue) claims.Add(new Claim("employeeId", employeeId.Value.ToString()));

        if (roles != null)
        {
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "JwtCustom"));
    }

    private string? ExtractToken(string authHeader)
    {
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return authHeader["Bearer ".Length..].Trim();
    }
}

public static class HttpContextExtensions
{
    public static string? GetUserId(this HttpContext context) => context.Items["UserId"]?.ToString();
    public static string? GetUserEmail(this HttpContext context) => context.Items["UserEmail"]?.ToString();
    public static int? GetEmployeeId(this HttpContext context) => context.Items.TryGetValue("EmployeeId", out var v) && v is int id ? id : null;
}