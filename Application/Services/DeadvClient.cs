using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Trainings;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public sealed class DeadvClient : IDeadvClient
{
    private const int MaxPdfBytes = 15 * 1024 * 1024;
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    private readonly HttpClient _http;
    private readonly ILogger<DeadvClient> _logger;

    public DeadvClient(HttpClient http, ILogger<DeadvClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<DeadvCertificateDto>?> GetCertificatesAsync(string idCard, CancellationToken ct = default)
    {
        if (_http.BaseAddress is null)
        {
            _logger.LogWarning("Deadv:ApiUrl no configurado. Consulta a DEaDV omitida.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(idCard)) return Array.Empty<DeadvCertificateDto>();

        try
        {
            var body = JsonSerializer.Serialize(new
            {
                fx = "gtcrtspblc",
                d = new { cid = idCard.Trim() },
                u = "none",
                rt = "etp",
                sr = new { ir = 1, er = 20, rp = 20, ip = "true" },
            });
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(string.Empty, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DEaDV respondió {Status} al consultar certificados.", (int)response.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return Parse(doc.RootElement);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (ct.IsCancellationRequested) throw;
            _logger.LogWarning(ex, "No se pudo consultar DEaDV.");
            return null;
        }
    }

    public async Task<byte[]?> DownloadCertificatePdfAsync(string certificateUrl, CancellationToken ct = default)
    {
        if (_http.BaseAddress is null
            || !Uri.TryCreate(certificateUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, _http.BaseAddress.Host, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Se rechazó una URL de certificado fuera del host permitido de DEaDV.");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return null;

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return null;
            if (response.Content.Headers.ContentLength > MaxPdfBytes) return null;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > MaxPdfBytes) return null;
                buffer.Write(chunk, 0, read);
            }

            var bytes = buffer.ToArray();
            // Firma %PDF: no basta confiar en el Content-Type del servidor remoto.
            return bytes.Length > 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46 ? bytes : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            if (ct.IsCancellationRequested) throw;
            _logger.LogWarning(ex, "No se pudo descargar el certificado desde DEaDV.");
            return null;
        }
    }

    private IReadOnlyList<DeadvCertificateDto>? Parse(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object) return null;
        if (data.TryGetProperty("success", out var ok) && ok.ValueKind == JsonValueKind.False) return null;

        // item puede venir como {} cuando la persona no tiene certificados.
        if (!data.TryGetProperty("item", out var item) || item.ValueKind != JsonValueKind.Object
            || !item.TryGetProperty("certs", out var certs) || certs.ValueKind != JsonValueKind.Array)
            return Array.Empty<DeadvCertificateDto>();

        var result = new List<DeadvCertificateDto>();
        foreach (var c in certs.EnumerateArray())
        {
            var code = Str(c, "cert_code");
            var name = Str(c, "cert_nombre_evento");
            var start = Date(c, "cert_fecha_inicio");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || start is null)
            {
                _logger.LogWarning("Certificado de DEaDV omitido por datos incompletos.");
                continue;
            }
            var end = Date(c, "cert_fecha_final");
            // El CHECK de la tabla exige EndDate >= StartDate.
            if (end is null || end < start) end = start;

            result.Add(new DeadvCertificateDto(
                code.Trim(), Str(c, "cert_file"), Str(c, "cert_codvalidador"), Str(c, "area"),
                name.Trim(), Str(c, "cert_tipo_evento"), Str(c, "cert_tipo_aprobacion"), Str(c, "cert_modalidad"),
                Str(c, "dependencia"), start.Value, end.Value, Hours(c)));
        }
        return result;
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static DateOnly? Date(JsonElement e, string name) =>
        DateTime.TryParseExact(Str(e, name), DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? DateOnly.FromDateTime(d) : null;

    private static int Hours(JsonElement e)
    {
        if (!e.TryGetProperty("cert_horas", out var v)) return 0;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return Math.Max(n, 0);
        return int.TryParse(v.ValueKind == JsonValueKind.String ? v.GetString() : null, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)
            ? Math.Max(s, 0) : 0;
    }
}
