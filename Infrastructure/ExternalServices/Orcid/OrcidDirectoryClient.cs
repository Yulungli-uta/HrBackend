using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;

namespace WsUtaSystem.Infrastructure.ExternalServices.Orcid;

/// <summary>
/// Consulta <c>/person</c> de la API pública de ORCID para saber si un iD existe y qué nombre publica. Solo se envía el
/// ORCID (ya validado con formato y dígito verificador, así que no puede alterar la ruta). El resultado se guarda unos
/// minutos para no repetir la consulta; un fallo del servicio no se guarda.
/// </summary>
public sealed class OrcidDirectoryClient : IOrcidDirectory
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrcidDirectoryClient> _logger;

    public OrcidDirectoryClient(HttpClient http, IMemoryCache cache, ILogger<OrcidDirectoryClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OrcidLookupResult> LookupAsync(string orcid, CancellationToken ct)
    {
        var key = "orcid-lookup:" + orcid;
        if (_cache.TryGetValue(key, out OrcidLookupResult? cached) && cached is not null) return cached;

        var result = await QueryAsync(orcid, ct);
        if (result.Status != OrcidLookupStatus.Unavailable) _cache.Set(key, result, CacheLifetime);
        return result;
    }

    private async Task<OrcidLookupResult> QueryAsync(string orcid, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"v3.0/{Uri.EscapeDataString(orcid)}/person");
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            using var response = await _http.SendAsync(request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return new OrcidLookupResult(OrcidLookupStatus.NotFound);
            if (!response.IsSuccessStatusCode) return new OrcidLookupResult(OrcidLookupStatus.Unavailable);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.Object
                ? new OrcidLookupResult(OrcidLookupStatus.Found, Value(name, "given-names"), Value(name, "family-name"))
                : new OrcidLookupResult(OrcidLookupStatus.Found);   // existe, pero no publica su nombre
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo consultar el registro público de ORCID.");
            return new OrcidLookupResult(OrcidLookupStatus.Unavailable);
        }
    }

    private static string? Value(JsonElement name, string property) =>
        name.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.Object &&
        p.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()!.Trim()
            : null;
}
