using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Dinardap;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.ExternalServices.Common;

namespace WsUtaSystem.Infrastructure.ExternalServices.Dinardap;

/// <summary>
/// Typed HttpClient hacia WsUtaDinardap.Api. La URL base se lee del parámetro DINARDAP_API_URL
/// (HR.tbl_Parameters) y solo se acepta si es HTTPS de un host uta.edu.ec. Nunca lanza excepción al llamador: cualquier falla (red, timeout,
/// permiso rechazado, DINARDAP caído) se registra como advertencia y retorna null/vacío —
/// mismo patrón que EmployeeProvisioningClient. Un DTO no-null (aunque tenga propiedades
/// internas en null) significa que el servicio SÍ respondió; null significa que no se pudo
/// consultar en absoluto — esa distinción es la que usa el llamador para decidir si bloquea
/// campo por campo o deja todo el formulario editable.
/// Cada consulta queda auditada (<see cref="IDinardapQueryAuditService"/>): quién la pidió, el paquete, la identificación
/// enmascarada y el resultado.
/// Identidad: las consultas de pantalla reenvían el token del USUARIO (necesita el rol del paquete, R_DINARDAP_REGISTRO_CIVIL o R_DINARDAP_TITULOS; sin él DINARDAP responde 403
/// y se informa como "sin permiso", sin caer a la cuenta de servicio). Los procesos masivos o en segundo plano usan la cuenta de servicio.
/// La opción Dinardap:ForwardUserToken=false devuelve las pantallas a la cuenta de servicio sin republicar.
/// </summary>
public sealed class HrDinardapClient : IHrDinardapClient
{
    private const string ServiceTokenCacheKey = "HrDinardapClient_ServiceToken";
    private const string ApiUrlParameterName = "DINARDAP_API_URL";
    private const string ForwardUserTokenSetting = "Dinardap:ForwardUserToken";

    private readonly HttpClient _http;
    private readonly ExternalServiceUrlResolver _urls;
    private readonly IEmployeeProvisioningClient _serviceAuth;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HrDinardapClient> _logger;
    private readonly IDinardapQueryAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IConfiguration _config;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HrDinardapClient(
        HttpClient http, ExternalServiceUrlResolver urls, IEmployeeProvisioningClient serviceAuth, IMemoryCache cache,
        ILogger<HrDinardapClient> logger, IDinardapQueryAuditService audit, ICurrentUserService currentUser,
        IHttpContextAccessor httpContext, IConfiguration config)
    {
        _http        = http        ?? throw new ArgumentNullException(nameof(http));
        _urls        = urls        ?? throw new ArgumentNullException(nameof(urls));
        _serviceAuth = serviceAuth ?? throw new ArgumentNullException(nameof(serviceAuth));
        _cache       = cache       ?? throw new ArgumentNullException(nameof(cache));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
        _audit       = audit       ?? throw new ArgumentNullException(nameof(audit));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        _config      = config      ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Cachea el JWT de servicio por 45 min (el token dura 60, ver AuthService.TokenExpirationMin
    /// típico) - evita un login completo contra RepositoryUta por CADA persona en una
    /// sincronización masiva (ej. 1426 empleados activos = 1426 logins innecesarios sin esto).
    /// </summary>
    private async Task<string?> GetCachedServiceTokenAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(ServiceTokenCacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var token = await _serviceAuth.GetServiceTokenAsync(ct);
        if (!string.IsNullOrWhiteSpace(token))
            _cache.Set(ServiceTokenCacheKey, token, TimeSpan.FromMinutes(45));

        return token;
    }

    // Los métodos sin identidad explícita conservan el comportamiento original: cuenta de servicio y null/vacío ante cualquier falla.
    public async Task<DinardapRegistroCivilDto?> ConsultarRegistroCivilAsync(string identificacion, CancellationToken ct = default) =>
        (await ConsultarRegistroCivilConResultadoAsync(identificacion, DinardapIdentity.Service, ct)).Data;

    public async Task<DinardapTceDto?> ConsultarTceAsync(string identificacion, CancellationToken ct = default) =>
        (await QueryAsync<DinardapTceDto>(DinardapPackages.Tce, identificacion,
            $"api/v1/dinardap/tce/{Uri.EscapeDataString(identificacion)}", DinardapIdentity.Service, ct)).Data;

    public async Task<IReadOnlyList<DinardapTituloDto>> ConsultarTitulosAsync(string identificacion, CancellationToken ct = default) =>
        (await ConsultarTitulosConResultadoAsync(identificacion, DinardapIdentity.Service, ct)).Data ?? new List<DinardapTituloDto>();

    public Task<DinardapLookupResult<DinardapRegistroCivilDto>> ConsultarRegistroCivilConResultadoAsync(
        string identificacion, DinardapIdentity identity = DinardapIdentity.CurrentUser, CancellationToken ct = default) =>
        QueryAsync<DinardapRegistroCivilDto>(DinardapPackages.RegistroCivil, identificacion,
            $"api/v1/dinardap/registro-civil/{Uri.EscapeDataString(identificacion)}", identity, ct);

    public async Task<DinardapLookupResult<IReadOnlyList<DinardapTituloDto>>> ConsultarTitulosConResultadoAsync(
        string identificacion, DinardapIdentity identity = DinardapIdentity.CurrentUser, CancellationToken ct = default)
    {
        var result = await QueryAsync<List<DinardapTituloDto>>(DinardapPackages.Titulos, identificacion,
            $"api/v1/dinardap/titulos/{Uri.EscapeDataString(identificacion)}", identity, ct);
        return new DinardapLookupResult<IReadOnlyList<DinardapTituloDto>>(result.Data, result.Status);
    }

    /// <summary>Consulta a DINARDAP y deja constancia de quién la pidió, con el resultado.</summary>
    private async Task<DinardapLookupResult<T>> QueryAsync<T>(
        string package, string identification, string relativeUrl, DinardapIdentity identity, CancellationToken ct) where T : class
    {
        var timer = Stopwatch.StartNew();
        var result = new DinardapLookupResult<T>(null, DinardapLookupStatus.Unavailable);
        try
        {
            result = await FetchAsync<T>(relativeUrl, identity, ct);
            return result;
        }
        finally
        {
            timer.Stop();
            await AuditAsync(package, identification, result.Status, (int)timer.ElapsedMilliseconds);
        }
    }

    private async Task AuditAsync(string package, string identification, DinardapLookupStatus status, int durationMs)
    {
        // Usuario conectado (pantalla) o proceso del sistema (trabajo programado, sin HttpContext autenticado).
        var isUser = _currentUser.IsAuthenticated;
        var route = (_httpContext.HttpContext?.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;

        await _audit.RecordAsync(new DinardapQueryRecord(
            Origin: isUser ? DinardapQueryOrigins.User : DinardapQueryOrigins.System,
            UserId: isUser ? _currentUser.UserId : null,
            UserEmail: isUser ? _currentUser.Email : null,
            EmployeeId: isUser ? _currentUser.EmployeeId : null,
            Package: package,
            Identification: identification,
            Outcome: status switch
            {
                DinardapLookupStatus.Ok => DinardapQueryOutcomes.Ok,
                DinardapLookupStatus.Forbidden => DinardapQueryOutcomes.Forbidden,
                _ => DinardapQueryOutcomes.Unavailable,
            },
            DurationMs: durationMs,
            SourceIp: isUser ? _currentUser.GetRemoteIp() : null,
            // Plantilla de la ruta (p. ej. .../dinardap-lookup/{cedula}): nunca la ruta real, que llevaría la identificación completa.
            Context: route ?? (isUser ? null : "Proceso del sistema")),
            // Sin el token de la solicitud: la consulta a DINARDAP ya ocurrió, y el registro debe quedar aunque el cliente se desconecte.
            CancellationToken.None);
    }

    /// <summary>
    /// Token Bearer del usuario de la solicitud en curso, o null si no hay usuario autenticado (trabajos en segundo plano) o la
    /// opción Dinardap:ForwardUserToken está desactivada (por defecto activa).
    /// </summary>
    private string? GetUserBearerToken()
    {
        if (bool.TryParse(_config[ForwardUserTokenSetting], out var forward) && !forward) return null;
        if (!_currentUser.IsAuthenticated) return null;

        var header = _httpContext.HttpContext?.Request.Headers.Authorization.ToString();
        const string scheme = "Bearer ";
        return !string.IsNullOrEmpty(header) && header.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)
            ? header[scheme.Length..].Trim()
            : null;
    }

    private async Task<DinardapLookupResult<T>> FetchAsync<T>(string relativeUrl, DinardapIdentity identity, CancellationToken ct) where T : class
    {
        var unavailable = new DinardapLookupResult<T>(null, DinardapLookupStatus.Unavailable);

        var baseUrl = await _urls.ResolveAsync(ApiUrlParameterName, ct, allowLoopbackInDevelopment: true);
        if (baseUrl is null)
        {
            _logger.LogWarning("{Name} no configurado o inválido. Consulta a DINARDAP omitida.", ApiUrlParameterName);
            return unavailable;
        }

        // Pantallas: token del usuario. Procesos masivos/en segundo plano (o sin usuario): cuenta de servicio.
        var userToken = identity == DinardapIdentity.CurrentUser ? GetUserBearerToken() : null;
        var usingUserToken = userToken is not null;
        var token = usingUserToken ? userToken : await GetCachedServiceTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("No se pudo obtener token de servicio para consultar DINARDAP ({Url}).", relativeUrl);
            return unavailable;
        }

        try
        {
            var response = await SendAsync(baseUrl, relativeUrl, token, ct);

            // El token de servicio cacheado pudo expirar a mitad de una corrida larga (ej. sincronización
            // masiva) - un solo reintento con token fresco antes de rendirse. El token del usuario NO se reintenta
            // con la cuenta de servicio: sería saltarse el control de permisos de la persona.
            if (!usingUserToken && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _cache.Remove(ServiceTokenCacheKey);
                var freshToken = await GetCachedServiceTokenAsync(ct);
                if (!string.IsNullOrWhiteSpace(freshToken))
                    response = await SendAsync(baseUrl, relativeUrl, freshToken, ct);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "WsUtaDinardap.Api respondió {Status} para {Url} ({Identity}): {Body}",
                        (int)response.StatusCode, relativeUrl, usingUserToken ? "token del usuario" : "cuenta de servicio", body);

                    // Solo con el token del usuario un 403 significa "esta persona no tiene el permiso".
                    return usingUserToken && response.StatusCode == System.Net.HttpStatusCode.Forbidden
                        ? new DinardapLookupResult<T>(null, DinardapLookupStatus.Forbidden)
                        : unavailable;
                }

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                return new DinardapLookupResult<T>(data, data is null ? DinardapLookupStatus.Unavailable : DinardapLookupStatus.Ok);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando WsUtaDinardap.Api ({Url}).", relativeUrl);
            return unavailable;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Uri baseUrl, string relativeUrl, string token, CancellationToken ct)
    {
        // La ruta base lleva un segmento (p. ej. /WsUtaDinardap): sin la barra final, Uri lo descartaría al combinar.
        var root = new UriBuilder(baseUrl) { Path = baseUrl.AbsolutePath.TrimEnd('/') + "/" }.Uri;
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(root, relativeUrl));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request, ct);
    }
}
