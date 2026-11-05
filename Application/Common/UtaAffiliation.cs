using System.Globalization;
using System.Text;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Reconoce a la Universidad Técnica de Ambato en el texto de una afiliación (Scopus u otros proveedores). Compara sin tildes
/// ni mayúsculas y tolera variantes: nombre en inglés ("Technical University of Ambato") y texto con la codificación dañada
/// que a veces devuelve el proveedor ("TÃľcnica"). No usa la sigla "UTA" sola (también es la de la Universidad de Texas en
/// Arlington) y descarta las demás instituciones de Ambato (Indoamérica, PUCE sede Ambato, UNIANDES…).
/// </summary>
public static class UtaAffiliation
{
    public static bool IsUta(string? affiliationName)
    {
        var text = Fold(affiliationName);
        if (text.Length == 0 || !text.Contains("ambato", StringComparison.Ordinal)) return false;

        var isUniversity = text.Contains("universidad", StringComparison.Ordinal) || text.Contains("university", StringComparison.Ordinal);
        var isTechnical = text.Contains("cnica", StringComparison.Ordinal) || text.Contains("technical", StringComparison.Ordinal);
        return isUniversity && isTechnical;
    }

    /// <summary>
    /// true si alguna afiliación es la UTA; false si hay afiliaciones y ninguna lo es; null (desconocido) si el proveedor no
    /// informa afiliaciones.
    /// </summary>
    public static bool? Resolve(IReadOnlyCollection<string> affiliationNames)
    {
        if (affiliationNames is null || affiliationNames.Count == 0) return null;
        return affiliationNames.Any(IsUta);
    }

    private static string Fold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().ToLowerInvariant();
    }
}
