using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Infrastructure.Services;

namespace WsUtaSystem.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly bool _enableLogging;
    private readonly HashSet<string> _publicPaths;

    private static readonly string[] SkipEmployeeLookupMarkers =
    {
        "/vw/employeedetails",
        "/api/v1/rh/vw/employeedetails"
    };

    private const string SkipLookupFlagKey = "__skip_employee_lookup";

    // Claim emitido por RepositoryUta (JwtTokenService) con el EmployeeID de RH
    private const string EmployeeIdClaimType = "employeeId";

    // TTL del caché email → EmployeeID usado solo como fallback para tokens sin claim
    private static readonly TimeSpan EmailLookupCacheDuration = TimeSpan.FromMinutes(5);

    // TTL del caché compartido de detalles (misma vigencia que usa CurrentUserService)
    private readonly TimeSpan _employeeDetailsCacheDuration;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _employeeDetailsCacheDuration = EmployeeDetailsCache.GetDuration(configuration);

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

            // 4. Resolución de Servicios
            var tokenValidationService = context.RequestServices.GetRequiredService<ITokenValidationService>();

            // 5. Validación de Token
            var validationResult = await tokenValidationService.ValidateTokenAsync(token);

            if (!validationResult.IsValid)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Token inválido", message = validationResult.Message });
                return;
            }

            // 6. Inyección de Claims y datos básicos en el Contexto
            context.Items["UserId"] = validationResult.UserId;
            context.Items["UserEmail"] = validationResult.Email;
            context.Items["UserRoles"] = validationResult.Roles;

            // 7. Resolución de EmployeeId: primero desde el claim firmado del token
            //    (sin tocar BD); fallback a la vista por email solo para tokens
            //    emitidos antes de que RepositoryUta incluyera el claim.
            int? employeeId = TryGetEmployeeIdFromToken(token);

            var skipLookup = ShouldSkipEmployeeLookup(path) || (context.Items.TryGetValue(SkipLookupFlagKey, out var f) && f is true);

            if (employeeId is null && !skipLookup && !string.IsNullOrWhiteSpace(validationResult.Email))
            {
                context.Items[SkipLookupFlagKey] = true;
                employeeId = await ResolveEmployeeIdByEmailAsync(context, validationResult.Email);
            }

            context.Items["EmployeeId"] = employeeId;

            // 8. Construir el Principal para el sistema de seguridad de .NET
            context.User = BuildPrincipal(validationResult.Email, validationResult.UserId, validationResult.Roles, employeeId);

            // 9. NUEVO: Cargar detalles del empleado en caché para que DepartmentID esté disponible
            if (employeeId.HasValue)
            {
                try
                {
                    var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
                    var meDetails = await currentUserService.LoadMeAsync(context.RequestAborted);

                    if (meDetails is not null)
                    {
                        // Inyectar nombre completo en el principal para que esté disponible
                        // en context.User.Identity.Name a lo largo de toda la cadena (reportes, auditoría, etc.)
                        if (context.User.Identity is ClaimsIdentity ci && !string.IsNullOrWhiteSpace(meDetails.FullName))
                            ci.AddClaim(new Claim(ClaimTypes.Name, meDetails.FullName));

                        if (_enableLogging)
                            _logger.LogInformation(
                                "[AUTH-MW] Detalles del empleado cargados: EmployeeId={EmployeeId} | Nombre={Name} | Departamento={Dept}",
                                employeeId, meDetails.FullName, meDetails.Department);
                    }
                    else
                    {
                        _logger.LogWarning("[AUTH-MW] No se encontraron detalles para empleado {EmployeeId}", employeeId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTH-MW] Error cargando detalles del empleado {EmployeeId}", employeeId);
                    // No lanzar excepción aquí, permitir que continúe
                }
            }

            // 10. Continuar la cadena
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH-MW] Error crítico en el pipeline de autenticación");
            throw;
        }
        finally
        {
            swTotal.Stop();
            if (_enableLogging)
                _logger.LogInformation("[AUTH-MW] Fin de procesamiento. Path: {Path} Status: {Status} Time: {Elapsed}ms",
                    path, context.Response.StatusCode, swTotal.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Extrae el claim "employeeId" del payload del JWT ya validado por el servicio de auth.
    /// No re-valida la firma: la validez del token quedó garantizada en el paso anterior.
    /// Retorna null si el claim no existe (tokens antiguos o usuarios sin empleado de RH).
    /// </summary>
    private int? TryGetEmployeeIdFromToken(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var claimValue = jwt.Claims.FirstOrDefault(c => c.Type == EmployeeIdClaimType)?.Value;
            return int.TryParse(claimValue, out var id) ? id : null;
        }
        catch (ArgumentException ex)
        {
            // Token válido para el servicio de auth pero no parseable localmente como JWT:
            // se continúa con el fallback por email sin interrumpir el request
            _logger.LogWarning(ex, "[AUTH-MW] No se pudo leer el payload del token; se usará fallback por email");
            return null;
        }
    }

    /// <summary>
    /// Fallback: resuelve el EmployeeID consultando vw_EmployeeDetails por email,
    /// con caché en memoria de <see cref="EmailLookupCacheDuration"/> para no repetir
    /// la consulta en cada request del mismo usuario.
    /// </summary>
    private async Task<int?> ResolveEmployeeIdByEmailAsync(HttpContext context, string email)
    {
        var cacheKey = $"auth:employee-id:{email.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out int cachedId))
            return cachedId;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var employeeDetailsService = context.RequestServices.GetRequiredService<IvwEmployeeDetailsService>();
            var emp = await employeeDetailsService.GetByEmailAsync(email, cts.Token);

            if (emp is not null)
            {
                var employeeId = emp.EmployeeID;
                _cache.Set(cacheKey, employeeId, EmailLookupCacheDuration);

                // Unificación: la consulta por email ya trajo la fila COMPLETA de la vista.
                // Se siembra el caché compartido de detalles (misma clave y TTL que usa
                // CurrentUserService) para que LoadMeAsync no repita la misma consulta.
                _cache.Set(EmployeeDetailsCache.KeyFor(employeeId), emp, _employeeDetailsCacheDuration);

                return employeeId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH-MW] Error buscando empleado para {Email}", email);
        }

        return null;
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