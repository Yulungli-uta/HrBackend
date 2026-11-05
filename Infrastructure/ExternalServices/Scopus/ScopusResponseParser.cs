using System.Globalization;
using System.Text.Json;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;

namespace WsUtaSystem.Infrastructure.ExternalServices.Scopus;

/// <summary>
/// Transforma la respuesta de Scopus Search (JSON, vista STANDARD) al formato común. Solo lee los campos documentados por
/// Elsevier; un registro sin identificador o sin título se omite (el servicio lo informa como incompleto).
/// </summary>
public static class ScopusResponseParser
{
    private const string RecordUrlTemplate = "https://www.scopus.com/inward/record.uri?partnerID=HzOxMe3b&scp={0}&origin=inward";

    /// <summary>
    /// Subtipos de Scopus que se importan como publicación: artículo (ar), revisión (re), ponencia (cp), revisión de congreso (cr)
    /// y capítulo de libro (ch). Los libros (bk), cartas, editoriales, notas, correcciones, etc. no se importan: los libros tienen
    /// su propio módulo. Se decide por el subtipo y nunca por <c>prism:aggregationType</c>: una ponencia publicada en una
    /// "Book Series" (p. ej. Lecture Notes) es una ponencia, no un libro.
    /// </summary>
    private static readonly HashSet<string> ImportedSubtypes = new(StringComparer.OrdinalIgnoreCase) { "ar", "re", "cp", "cr", "ch" };

    public sealed record Page(int? TotalResults, int EntryCount, IReadOnlyList<ExternalPublication> Items, int SkippedByType = 0);

    public static Page Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("search-results", out var results))
            return new Page(null, 0, Array.Empty<ExternalPublication>());

        int? total = null;
        if (results.TryGetProperty("opensearch:totalResults", out var t) &&
            int.TryParse(t.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            total = parsed;

        var items = new List<ExternalPublication>();
        var entryCount = 0;
        var skippedByType = 0;
        if (results.TryGetProperty("entry", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entries.EnumerateArray())
            {
                // Sin resultados Scopus devuelve una entrada {"error": "Result set was empty"}.
                if (entry.TryGetProperty("error", out _)) continue;
                entryCount++;

                var item = ParseEntry(entry);
                if (item is null) continue;

                // Sin subtipo informado no se descarta: se prefiere importarlo a perderlo en silencio.
                if (item.SubtypeCode is { Length: > 0 } subtype && !ImportedSubtypes.Contains(subtype))
                {
                    skippedByType++;
                    continue;
                }
                items.Add(item);
            }
        }

        return new Page(total, entryCount, items, skippedByType);
    }

    private static ExternalPublication? ParseEntry(JsonElement e)
    {
        var id = ScopusId(Str(e, "dc:identifier"), Str(e, "eid"));
        var title = Str(e, "dc:title");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title)) return null;

        var affiliations = new List<string>();
        if (e.TryGetProperty("affiliation", out var aff) && aff.ValueKind == JsonValueKind.Array)
            foreach (var a in aff.EnumerateArray())
                if (Str(a, "affilname") is { Length: > 0 } name) affiliations.Add(name);

        return new ExternalPublication(
            ExternalId: id,
            Title: title.Trim(),
            JournalName: Str(e, "prism:publicationName"),
            Issn: Str(e, "prism:issn"),
            Isbn: FirstIsbn(e),
            Volume: Str(e, "prism:volume"),
            Issue: Str(e, "prism:issueIdentifier"),
            Pages: Str(e, "prism:pageRange") ?? Str(e, "article-number"),
            PublicationDate: ParseDate(Str(e, "prism:coverDate")),
            Doi: Str(e, "prism:doi"),
            SubtypeCode: Str(e, "subtype"),
            AggregationType: Str(e, "prism:aggregationType"),
            AffiliationNames: affiliations,
            RecordUrl: string.Format(CultureInfo.InvariantCulture, RecordUrlTemplate, id),
            IsIndexed: true);
    }

    /// <summary>ID de Scopus: <c>dc:identifier</c> viene como "SCOPUS_ID:123"; si falta se toma del EID ("2-s2.0-123").</summary>
    public static string? ScopusId(string? identifier, string? eid)
    {
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            var idx = identifier.IndexOf(':');
            var value = (idx >= 0 ? identifier[(idx + 1)..] : identifier).Trim();
            if (value.Length > 0) return value;
        }
        if (!string.IsNullOrWhiteSpace(eid))
        {
            var idx = eid.LastIndexOf('-');
            var value = (idx >= 0 ? eid[(idx + 1)..] : eid).Trim();
            if (value.Length > 0) return value;
        }
        return null;
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? (string.IsNullOrWhiteSpace(v.GetString()) ? null : v.GetString()!.Trim())
            : null;

    /// <summary>El ISBN llega como texto o como lista de objetos <c>{"$": "..."}</c>.</summary>
    private static string? FirstIsbn(JsonElement e)
    {
        if (!e.TryGetProperty("prism:isbn", out var v)) return null;
        if (v.ValueKind == JsonValueKind.String) return v.GetString();
        if (v.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in v.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String) return item.GetString();
                if (item.TryGetProperty("$", out var text) && text.ValueKind == JsonValueKind.String) return text.GetString();
            }
        }
        return null;
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
