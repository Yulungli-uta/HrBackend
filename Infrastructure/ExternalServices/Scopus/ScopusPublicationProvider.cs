using System.Globalization;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;
using WsUtaSystem.Infrastructure.ExternalServices.Common;

namespace WsUtaSystem.Infrastructure.ExternalServices.Scopus;

/// <summary>
/// Adaptador de Elsevier (Scopus Search, vista STANDARD). La URL y las credenciales viven en Parámetros del Sistema
/// (PUBSYNC_SCOPUS_*) y viajan solo por headers (X-ELS-APIKey, X-ELS-Insttoken): nunca en la URL, en logs ni en el navegador.
/// Recorre las páginas de 25 con <c>start</c>; los fallos del servicio se devuelven como resultado, no como excepción.
/// </summary>
public sealed class ScopusPublicationProvider : IPublicationSourceProvider
{
    public const string UrlParameter = "PUBSYNC_SCOPUS_API_URL";
    public const string ApiKeyParameter = "PUBSYNC_SCOPUS_API_KEY";
    public const string InstTokenParameter = "PUBSYNC_SCOPUS_INST_TOKEN";

    private const string AllowedHost = "api.elsevier.com";
    private const int PageSize = 25;          // máximo permitido para este nivel de servicio (más da 400)
    private const int MaxPages = 80;          // tope de seguridad: 2.000 documentos por investigador
    private const int MaxStart = 5000;        // Scopus no permite start+count > 5.000 sin cursor
    private static readonly TimeSpan PageDelay = TimeSpan.FromMilliseconds(150); // límite de 9 consultas por segundo
    private const string Fields =
        "dc:identifier,eid,dc:title,prism:publicationName,prism:issn,prism:isbn,prism:volume,prism:issueIdentifier," +
        "prism:pageRange,article-number,prism:coverDate,prism:doi,subtype,prism:aggregationType,affiliation";

    private readonly HttpClient _http;
    private readonly IParametersService _parameters;
    private readonly ILogger<ScopusPublicationProvider> _logger;

    public ScopusPublicationProvider(HttpClient http, IParametersService parameters, ILogger<ScopusPublicationProvider> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProviderCode => "SCOPUS";

    public async Task<bool> IsConfiguredAsync(CancellationToken ct) => await LoadConfigAsync(ct) is not null;

    public async Task<ExternalPublicationsResult> FetchPublicationsAsync(ExternalAuthorRef author, CancellationToken ct)
    {
        var config = await LoadConfigAsync(ct);
        if (config is null)
            return Unavailable("La consulta a Scopus no está configurada (URL y API Key en Parámetros del Sistema).", ExternalErrorKinds.Config);

        var query = BuildQuery(author);
        if (query is null)
            return Unavailable("No hay un Scopus Author ID ni un ORCID para consultar.", ExternalErrorKinds.Config);

        var items = new List<ExternalPublication>();
        int? total = null;
        int? quota = null;
        var skipped = 0;
        var start = 0;

        for (var page = 0; page < MaxPages; page++)
        {
            ct.ThrowIfCancellationRequested();
            var (status, body, remaining) = await GetPageAsync(config, query, start, ct);
            if (remaining is not null) quota = remaining;

            if (status != 200 || body is null)
            {
                var (kind, message) = Describe(status);
                // Sin ninguna página recuperada no hay resultado; con algunas, es un resultado parcial.
                return items.Count == 0 && page == 0
                    ? Unavailable(message, kind, quota)
                    : new ExternalPublicationsResult(items, total, IsPartial: true, Available: true, message, quota, kind, skipped);
            }

            var parsed = ScopusResponseParser.Parse(body);
            total ??= parsed.TotalResults;
            items.AddRange(parsed.Items);
            skipped += parsed.SkippedByType;

            start += PageSize;
            if (parsed.EntryCount == 0 || total is null || start >= total.Value) break;
            if (start >= MaxStart)
                return new ExternalPublicationsResult(items, total, true, true,
                    "Scopus limita la consulta a 5.000 resultados; el resultado es parcial.", quota, SkippedByType: skipped);

            await Task.Delay(PageDelay, ct);
        }

        var partial = total is not null && items.Count + skipped < total.Value && start < total.Value;
        return new ExternalPublicationsResult(items, total, partial, true,
            partial ? "Se alcanzó el máximo de páginas por consulta; el resultado es parcial." : null, quota, SkippedByType: skipped);
    }

    /// <summary>Con IDs de autor: <c>AU-ID(a) OR AU-ID(b)</c>. Sin IDs, por ORCID. IDs y ORCID ya validados al registrarlos.</summary>
    public static string? BuildQuery(ExternalAuthorRef author)
    {
        var ids = author.ProviderAuthorIds.Where(id => !string.IsNullOrWhiteSpace(id) && id.All(char.IsAsciiDigit)).Distinct().ToList();
        if (ids.Count > 0) return string.Join(" OR ", ids.Select(id => $"AU-ID({id})"));
        return string.IsNullOrWhiteSpace(author.Orcid) ? null : $"ORCID({author.Orcid.Trim()})";
    }

    private async Task<(int Status, string? Body, int? Remaining)> GetPageAsync(Config config, string query, int start, CancellationToken ct)
    {
        var uri = new Uri(config.BaseUri,
            $"content/search/scopus?query={Uri.EscapeDataString(query)}&count={PageSize}&start={start}&field={Fields}");

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.TryAddWithoutValidation("X-ELS-APIKey", config.ApiKey);
                if (!string.IsNullOrEmpty(config.InstToken)) request.Headers.TryAddWithoutValidation("X-ELS-Insttoken", config.InstToken);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");

                using var response = await _http.SendAsync(request, ct);
                int? remaining = response.Headers.TryGetValues("X-RateLimit-Remaining", out var values) &&
                                 int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : null;
                var code = (int)response.StatusCode;
                var body = code == 200 ? await response.Content.ReadAsStringAsync(ct) : null;

                // 5xx es transitorio: un reintento. Cualquier otro código no mejora repitiendo.
                if (code >= 500 && attempt == 1) { await Task.Delay(TimeSpan.FromSeconds(1), ct); continue; }
                return (code, body, remaining);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                if (attempt == 2)
                {
                    _logger.LogWarning(ex, "No se pudo consultar Scopus.");
                    return (0, null, null);
                }
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
        return (0, null, null);
    }

    private static (string Kind, string Message) Describe(int status) => status switch
    {
        401 or 403 => (ExternalErrorKinds.Credentials, "Scopus rechazó las credenciales configuradas (API Key o Insttoken)."),
        429 => (ExternalErrorKinds.Quota, "Se agotó el cupo semanal de consultas a Scopus."),
        400 => (ExternalErrorKinds.Config, "Scopus rechazó la consulta (revise el ID de autor)."),
        0 => (ExternalErrorKinds.Network, "No se pudo conectar con Scopus. Intente de nuevo más tarde."),
        _ => (ExternalErrorKinds.Network, "Scopus no respondió correctamente. Intente de nuevo más tarde."),
    };

    private static ExternalPublicationsResult Unavailable(string message, string kind, int? quota = null) =>
        new(Array.Empty<ExternalPublication>(), null, false, false, message, quota, kind);

    private async Task<Config?> LoadConfigAsync(CancellationToken ct)
    {
        var url = await ParameterAsync(UrlParameter, ct);
        var key = await ParameterAsync(ApiKeyParameter, ct);
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key)) return null;

        if (!Uri.TryCreate(url.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) || !ExactHostUrlPolicy.IsAllowed(baseUri, AllowedHost))
        {
            _logger.LogWarning("El parámetro {Name} no es una URL HTTPS válida de {Host}.", UrlParameter, AllowedHost);
            return null;
        }

        return new Config(baseUri, key.Trim(), (await ParameterAsync(InstTokenParameter, ct))?.Trim());
    }

    private async Task<string?> ParameterAsync(string name, CancellationToken ct) =>
        (await _parameters.GetByNameAsync(name, ct)).Select(p => p.Pvalues?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));

    private sealed record Config(Uri BaseUri, string ApiKey, string? InstToken);
}
