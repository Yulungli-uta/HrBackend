namespace WsUtaSystem.Application.Common;

/// <summary>
/// Normaliza un DOI igual que la columna calculada HR.tbl_Publications.DoiNormalized: minúsculas, sin prefijo de URL
/// ni "doi:"; si el resultado no empieza con "10." (p. ej. una URL de ORCID o de una revista) no es un DOI y devuelve null.
/// </summary>
public static class DoiNormalizer
{
    private static readonly string[] Prefixes =
    {
        "https://doi.org/", "http://doi.org/", "https://dx.doi.org/", "http://dx.doi.org/", "doi.org/", "doi:",
    };

    public static string? Normalize(string? doi)
    {
        if (string.IsNullOrWhiteSpace(doi)) return null;

        var value = doi.Trim();
        foreach (var prefix in Prefixes)
            value = value.Replace(prefix, string.Empty, StringComparison.OrdinalIgnoreCase);

        value = value.Trim().ToLowerInvariant();
        return value.StartsWith("10.", StringComparison.Ordinal) ? value : null;
    }

    /// <summary>URL pública del DOI (<c>https://doi.org/…</c>) o null si no es un DOI válido.</summary>
    public static string? ToUrl(string? doi)
    {
        var normalized = Normalize(doi);
        return normalized is null ? null : $"https://doi.org/{normalized}";
    }
}
