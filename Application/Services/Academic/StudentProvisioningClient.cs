using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services.Academic;

namespace WsUtaSystem.Application.Services.Academic;

/// <summary>
/// Typed HttpClient que delega operaciones AD de estudiantes a RepositoryUta.
/// La URL base se lee de "AuthService:Url" en appsettings.json.
/// </summary>
public class StudentProvisioningClient : IStudentProvisioningClient
{
    private readonly HttpClient _http;
    private readonly ILogger<StudentProvisioningClient> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StudentProvisioningClient(HttpClient http, ILogger<StudentProvisioningClient> logger)
    {
        _http   = http   ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Crear cuenta AD ───────────────────────────────────────────────────────

    public async Task<CreateStudentAdAccountResult?> CreateAdAccountAsync(
        CreateStudentAdAccountRequest req,
        string bearerToken,
        CancellationToken ct = default)
    {
        if (_http.BaseAddress is null)
        {
            _logger.LogWarning(
                "RepositoryUta BaseUrl no configurado. Aprovisionamiento omitido para HrStudentId={Id}",
                req.HrStudentId);
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post, "api/academic/student-provisioning/students");

            SetBearer(request, bearerToken);
            request.Content = JsonContent.Create(req);

            using var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "CreateAdAccount rechazado para HrStudentId={Id}: HTTP {Status} — {Body}",
                    req.HrStudentId, (int)response.StatusCode, body);
                return new CreateStudentAdAccountResult(false, null, null, body);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data))
            {
                _logger.LogWarning(
                    "Respuesta sin campo 'data' para HrStudentId={Id}", req.HrStudentId);
                return null;
            }

            var adObjectId = data.TryGetProperty("adObjectId", out var adEl)   ? adEl.GetString()   : null;
            var email      = data.TryGetProperty("email",      out var emailEl) ? emailEl.GetString() : null;
            var errorMsg   = data.TryGetProperty("errorMessage", out var errEl) ? errEl.GetString()  : null;
            var success    = data.TryGetProperty("success",    out var sucEl)   && sucEl.GetBoolean();

            return new CreateStudentAdAccountResult(success, adObjectId, email, errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al llamar RepositoryUta/student-provisioning para HrStudentId={Id}", req.HrStudentId);
            return null;
        }
    }

    // ── Deshabilitar cuenta AD ────────────────────────────────────────────────

    public async Task<DisableStudentAdAccountResult?> DisableAdAccountAsync(
        string adObjectId,
        string bearerToken,
        CancellationToken ct = default)
    {
        if (_http.BaseAddress is null)
        {
            _logger.LogWarning(
                "RepositoryUta BaseUrl no configurado. Deshabilitar omitido para AdObjectId={Id}", adObjectId);
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/academic/student-provisioning/ad-accounts/{Uri.EscapeDataString(adObjectId)}/disable");

            SetBearer(request, bearerToken);

            using var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "DisableAdAccount rechazado para AdObjectId={Id}: HTTP {Status} — {Body}",
                    adObjectId, (int)response.StatusCode, body);
                return new DisableStudentAdAccountResult(false, adObjectId, body);
            }

            return new DisableStudentAdAccountResult(true, adObjectId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al llamar RepositoryUta/student-provisioning/disable para AdObjectId={Id}", adObjectId);
            return null;
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static void SetBearer(HttpRequestMessage request, string bearerToken)
    {
        var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerToken[7..]
            : bearerToken;

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
