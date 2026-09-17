using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Dinardap;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Typed HttpClient hacia WsUtaDinardap.Api. La URL base se lee de "Dinardap:ApiUrl" en
/// appsettings.json. Nunca lanza excepción al llamador: cualquier falla (red, timeout,
/// permiso rechazado, DINARDAP caído) se registra como advertencia y retorna null/vacío —
/// mismo patrón que EmployeeProvisioningClient. Un DTO no-null (aunque tenga propiedades
/// internas en null) significa que el servicio SÍ respondió; null significa que no se pudo
/// consultar en absoluto — esa distinción es la que usa el llamador para decidir si bloquea
/// campo por campo o deja todo el formulario editable.
/// </summary>
public sealed class HrDinardapClient : IHrDinardapClient
{
    private const string ServiceTokenCacheKey = "HrDinardapClient_ServiceToken";

    private readonly HttpClient _http;
    private readonly IEmployeeProvisioningClient _serviceAuth;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HrDinardapClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HrDinardapClient(
        HttpClient http, IEmployeeProvisioningClient serviceAuth, IMemoryCache cache, ILogger<HrDinardapClient> logger)
    {
        _http        = http        ?? throw new ArgumentNullException(nameof(http));
        _serviceAuth = serviceAuth ?? throw new ArgumentNullException(nameof(serviceAuth));
        _cache       = cache       ?? throw new ArgumentNullException(nameof(cache));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
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

    public Task<DinardapRegistroCivilDto?> ConsultarRegistroCivilAsync(string identificacion, CancellationToken ct = default) =>
        GetAsync<DinardapRegistroCivilDto>($"api/v1/dinardap/registro-civil/{Uri.EscapeDataString(identificacion)}", ct);

    public Task<DinardapTceDto?> ConsultarTceAsync(string identificacion, CancellationToken ct = default) =>
        GetAsync<DinardapTceDto>($"api/v1/dinardap/tce/{Uri.EscapeDataString(identificacion)}", ct);

    public async Task<IReadOnlyList<DinardapTituloDto>> ConsultarTitulosAsync(string identificacion, CancellationToken ct = default) =>
        await GetAsync<List<DinardapTituloDto>>($"api/v1/dinardap/titulos/{Uri.EscapeDataString(identificacion)}", ct)
        ?? new List<DinardapTituloDto>();

    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken ct) where T : class
    {
        if (_http.BaseAddress is null)
        {
            _logger.LogWarning("Dinardap:ApiUrl no configurado. Consulta a DINARDAP omitida.");
            return null;
        }

        var token = await GetCachedServiceTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("No se pudo obtener token de servicio para consultar DINARDAP ({Url}).", relativeUrl);
            return null;
        }

        try
        {
            var response = await SendAsync(relativeUrl, token, ct);

            // El token cacheado pudo expirar a mitad de una corrida larga (ej. sincronización
            // masiva) - un solo reintento con token fresco antes de rendirse.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _cache.Remove(ServiceTokenCacheKey);
                var freshToken = await GetCachedServiceTokenAsync(ct);
                if (!string.IsNullOrWhiteSpace(freshToken))
                    response = await SendAsync(relativeUrl, freshToken, ct);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "WsUtaDinardap.Api respondió {Status} para {Url}: {Body}",
                        (int)response.StatusCode, relativeUrl, body);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando WsUtaDinardap.Api ({Url}).", relativeUrl);
            return null;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUrl, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request, ct);
    }
}
