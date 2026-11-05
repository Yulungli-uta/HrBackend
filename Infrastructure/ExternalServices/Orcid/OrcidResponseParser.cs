using System.Text.Json;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;

namespace WsUtaSystem.Infrastructure.ExternalServices.Orcid;

/// <summary>
/// Transforma la respuesta de <c>/works</c> de la API pública de ORCID al formato común. Solo importa producción académica
/// (artículo, ponencia y capítulo de libro); libros, preprints, datos, software, informes, revistas de divulgación y "otros" se
/// cuentan como omitidos por tipo. Las obras vienen agrupadas (una por cada fuente que la declaró): se toma una representante
/// con una regla fija —la de menor código— para que repetir la sincronización produzca la misma clave.
/// </summary>
public static class OrcidResponseParser
{
    /// <summary>Tipo de ORCID → subtipo común (el mismo código que usa el resto del sincronizador).</summary>
    private static readonly Dictionary<string, string> ImportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["journal-article"] = "ar",
        ["conference-paper"] = "cp",
        ["book-chapter"] = "ch",
    };

    public sealed record Result(IReadOnlyList<ExternalPublication> Items, int SkippedByType, int TotalGroups);

    public static Result Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var items = new List<ExternalPublication>();
        var skipped = 0;
        var groups = 0;

        if (doc.RootElement.TryGetProperty("group", out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var group in array.EnumerateArray())
            {
                groups++;
                var summaries = Summaries(group);
                if (summaries.Count == 0) continue;

                var main = summaries[0];
                var type = Str(main, "type");
                if (type is null || !ImportedTypes.TryGetValue(type, out var subtype))
                {
                    skipped++;
                    continue;
                }

                var title = Nested(Nested(main, "title"), "title", "value");
                var putCode = PutCode(main);
                if (string.IsNullOrWhiteSpace(title) || putCode is null) continue;

                var ids = ExternalIds(group, main);
                items.Add(new ExternalPublication(
                    ExternalId: $"put:{putCode}",
                    Title: title,
                    JournalName: summaries.Select(s => Nested(s, "journal-title", "value")).FirstOrDefault(v => !string.IsNullOrEmpty(v)),
                    Issn: ids.GetValueOrDefault("issn"),
                    Isbn: ids.GetValueOrDefault("isbn"),
                    Volume: null,
                    Issue: null,
                    Pages: null,
                    PublicationDate: summaries.Select(DateOf).FirstOrDefault(d => d is not null),
                    Doi: ids.GetValueOrDefault("doi"),
                    SubtypeCode: subtype,
                    AggregationType: null,
                    AffiliationNames: Array.Empty<string>(),
                    RecordUrl: SafeUrl(summaries.Select(s => Nested(s, "url", "value")).FirstOrDefault(v => !string.IsNullOrEmpty(v))),
                    IsIndexed: null));
            }
        }

        return new Result(items, skipped, groups);
    }

    /// <summary>Resúmenes del grupo ordenados por código ascendente: el primero es el representante.</summary>
    private static List<JsonElement> Summaries(JsonElement group)
    {
        var list = new List<JsonElement>();
        if (group.TryGetProperty("work-summary", out var ws) && ws.ValueKind == JsonValueKind.Array)
            list.AddRange(ws.EnumerateArray());
        return list.OrderBy(s => PutCode(s) ?? long.MaxValue).ToList();
    }

    private static long? PutCode(JsonElement summary) =>
        summary.TryGetProperty("put-code", out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var v) ? v : null;

    /// <summary>Identificadores del grupo (DOI, ISSN, ISBN); si el grupo no los trae, los del resumen representante.</summary>
    private static Dictionary<string, string> ExternalIds(JsonElement group, JsonElement main)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in new[] { group, main })
        {
            if (!container.TryGetProperty("external-ids", out var ext) || ext.ValueKind != JsonValueKind.Object) continue;
            if (!ext.TryGetProperty("external-id", out var arr) || arr.ValueKind != JsonValueKind.Array) continue;
            foreach (var id in arr.EnumerateArray())
            {
                var type = Str(id, "external-id-type");
                var value = Str(id, "external-id-value");
                if (type is not null && value is not null && !result.ContainsKey(type)) result[type] = value;
            }
        }
        return result;
    }

    /// <summary>Fecha de publicación. ORCID permite año, año-mes o fecha completa; lo que falta se completa con 1.</summary>
    private static DateOnly? DateOf(JsonElement summary)
    {
        if (!summary.TryGetProperty("publication-date", out var d) || d.ValueKind != JsonValueKind.Object) return null;
        if (!int.TryParse(Nested(d, "year", "value"), out var year) || year is < 1000 or > 9999) return null;
        var month = int.TryParse(Nested(d, "month", "value"), out var m) && m is >= 1 and <= 12 ? m : 1;
        var day = int.TryParse(Nested(d, "day", "value"), out var dd) && dd >= 1 && dd <= DateTime.DaysInMonth(year, month) ? dd : 1;
        return new DateOnly(year, month, day);
    }

    /// <summary>Solo http(s): el enlace lo escribe un tercero y se muestra como hipervínculo.</summary>
    private static string? SafeUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.AbsoluteUri
            : null;

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? (string.IsNullOrWhiteSpace(v.GetString()) ? null : v.GetString()!.Trim())
            : null;

    private static string? Nested(JsonElement e, string a, string b)
    {
        var inner = Nested(e, a);
        return inner.ValueKind == JsonValueKind.Object ? Str(inner, b) : null;
    }

    private static JsonElement Nested(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : default;
}
