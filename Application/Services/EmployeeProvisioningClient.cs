using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Provisioning;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Typed HttpClient que delega el aprovisionamiento de empleados a RepositoryUta.
/// La URL base se lee de la sección "AuthService:Url" en appsettings.json.
/// </summary>
public class EmployeeProvisioningClient : IEmployeeProvisioningClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EmployeeProvisioningClient> _logger;
    private readonly IConfiguration _config;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EmployeeProvisioningClient(
        HttpClient http,
        ILogger<EmployeeProvisioningClient> logger,
        IConfiguration config)
    {
        _http   = http   ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<HrProvisioningResult?> ProvisionAsync(
        HrProvisionEmployeeRequest req,
        string bearerToken,
        CancellationToken ct = default)
    {
        if (_http.BaseAddress is null)
        {
            _logger.LogWarning("RepositoryUta BaseUrl no está configurado. Aprovisionamiento omitido para HrEmployeeId={Id}", req.HrEmployeeId);
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/provisioning/employees");

            var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? bearerToken[7..]
                : bearerToken;

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            request.Content = JsonContent.Create(req);

            _logger.LogInformation(
                "Iniciando llamada a RepositoryUta provisioning. BaseAddress={BaseAddress}, Endpoint={Endpoint}, HrEmployeeId={HrEmployeeId}, Email={Email}, DisplayName={DisplayName}, HasBearerToken={HasBearerToken}",
                _http.BaseAddress,
                "api/provisioning/employees",
                req.HrEmployeeId,
                req.Email,
                req.DisplayName,
                !string.IsNullOrWhiteSpace(bearerToken));

            using var response = await _http.SendAsync(request, ct);

            if ((int)response.StatusCode == 409)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogInformation(
                    "Aprovisionamiento omitido — cuenta ya existe para HrEmployeeId={Id}: {Body}",
                    req.HrEmployeeId, body);
                return new HrProvisioningResult(
                    Guid.Empty, req.HrEmployeeId, req.Email, 0, null, body, AlreadyExists: true);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Aprovisionamiento rechazado por RepositoryUta para HrEmployeeId={Id}: HTTP {Status} — {Body}",
                    req.HrEmployeeId, (int)response.StatusCode, body);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var dataEl))
            {
                _logger.LogWarning("Respuesta sin campo 'data' para HrEmployeeId={Id}", req.HrEmployeeId);
                return null;
            }

            return dataEl.Deserialize<HrProvisioningResult>(_jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al llamar RepositoryUta/provisioning para HrEmployeeId={Id}", req.HrEmployeeId);
            return null;
        }
    }

    public async Task<HrDisableEmployeeResult?> DisableAsync(
        int hrEmployeeId,
        string bearerToken,
        CancellationToken ct = default)
    {
        if (_http.BaseAddress is null)
        {
            _logger.LogWarning("RepositoryUta BaseUrl no configurado. Deshabilitar omitido para HrEmployeeId={Id}", hrEmployeeId);
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/provisioning/employees/{hrEmployeeId}/disable");

            var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? bearerToken[7..]
                : bearerToken;

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "DisableAsync rechazado por RepositoryUta para HrEmployeeId={Id}: HTTP {Status} — {Body}",
                    hrEmployeeId, (int)response.StatusCode, body);
                return new HrDisableEmployeeResult(false, hrEmployeeId, null, body);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var dataEl))
                return new HrDisableEmployeeResult(true, hrEmployeeId, null, null);

            return dataEl.Deserialize<HrDisableEmployeeResult>(_jsonOptions)
                   ?? new HrDisableEmployeeResult(true, hrEmployeeId, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al llamar RepositoryUta/disable para HrEmployeeId={Id}", hrEmployeeId);
            return null;
        }
    }

    public async Task<string?> GetServiceTokenAsync(CancellationToken ct = default)
    {
        var email    = _config["AuthService:ServiceAccount:Email"];
        var password = _config["AuthService:ServiceAccount:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
            || email == "VARIABLE_DE_ENTORNO")
        {
            _logger.LogWarning("AuthService:ServiceAccount no configurado. Token de servicio omitido.");
            return null;
        }

        if (_http.BaseAddress is null)
        {
            _logger.LogWarning("RepositoryUta BaseUrl no configurado. Token de servicio omitido.");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login");
            request.Content = JsonContent.Create(new { email, password });

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Login de cuenta de servicio fallido: HTTP {Status} — {Body}",
                    (int)response.StatusCode, body);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var dataEl)
                && dataEl.TryGetProperty("accessToken", out var tokenEl))
                return tokenEl.GetString();

            _logger.LogWarning("Respuesta de login de servicio sin campo data.accessToken.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener token de cuenta de servicio desde RepositoryUta.");
            return null;
        }
    }
}
