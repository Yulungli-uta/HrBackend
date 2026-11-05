using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;

namespace WsUtaSystem.Infrastructure.ExternalServices.Orcid;

/// <summary>
/// Adaptador de ORCID (API pública, lectura de <c>/works</c>). El servicio público es gratuito y no requiere credenciales ni
/// parámetros: la dirección es fija (<c>pub.orcid.org</c>) y se configura en el registro de HttpClient, no en Parámetros del
/// Sistema. Los fallos del servicio se devuelven como resultado, nunca como excepción.
/// </summary>
public sealed class OrcidPublicationProvider : IPublicationSourceProvider
{
    public const string Host = "pub.orcid.org";

    private readonly HttpClient _http;
    private readonly ILogger<OrcidPublicationProvider> _logger;

    public OrcidPublicationProvider(HttpClient http, ILogger<OrcidPublicationProvider> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProviderCode => "ORCID";

    /// <summary>La API pública no necesita configuración.</summary>
    public Task<bool> IsConfiguredAsync(CancellationToken ct) => Task.FromResult(true);

    public async Task<ExternalPublicationsResult> FetchPublicationsAsync(ExternalAuthorRef author, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(author.Orcid))
            return Unavailable("No hay un ORCID para consultar.", ExternalErrorKinds.Config);

        var (status, body) = await GetAsync($"v3.0/{Uri.EscapeDataString(author.Orcid.Trim())}/works", ct);

        // Un ORCID inexistente no es un fallo del servicio: no debe detener una corrida masiva.
        if (status == 404)
            return new ExternalPublicationsResult(Array.Empty<ExternalPublication>(), 0, false, true, "El ORCID no existe en orcid.org.");
        if (status != 200 || body is null)
        {
            var (kind, message) = Describe(status);
            return Unavailable(message, kind);
        }

        try
        {
            var parsed = OrcidResponseParser.Parse(body);
            return new ExternalPublicationsResult(parsed.Items, parsed.TotalGroups, false, true, null, SkippedByType: parsed.SkippedByType);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "La respuesta de ORCID no tiene el formato esperado.");
            return Unavailable("ORCID respondió con un formato inesperado. Intente de nuevo más tarde.", ExternalErrorKinds.Network);
        }
    }

    private async Task<(int Status, string? Body)> GetAsync(string path, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.TryAddWithoutValidation("Accept", "application/json"); // sin esto ORCID responde XML

                using var response = await _http.SendAsync(request, ct);
                var code = (int)response.StatusCode;
                var body = code == 200 ? await response.Content.ReadAsStringAsync(ct) : null;

                // 5xx es transitorio: un reintento. Cualquier otro código no mejora repitiendo.
                if (code >= 500 && attempt == 1) { await Task.Delay(TimeSpan.FromSeconds(1), ct); continue; }
                return (code, body);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                if (attempt == 2)
                {
                    _logger.LogWarning(ex, "No se pudo consultar ORCID.");
                    return (0, null);
                }
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
        return (0, null);
    }

    private static (string Kind, string Message) Describe(int status) => status switch
    {
        429 => (ExternalErrorKinds.Quota, "ORCID limitó las consultas (cupo diario de la API pública). Intente de nuevo más tarde."),
        0 => (ExternalErrorKinds.Network, "No se pudo conectar con ORCID. Intente de nuevo más tarde."),
        _ => (ExternalErrorKinds.Network, "ORCID no respondió correctamente. Intente de nuevo más tarde."),
    };

    private static ExternalPublicationsResult Unavailable(string message, string kind) =>
        new(Array.Empty<ExternalPublication>(), null, false, false, message, null, kind);
}
