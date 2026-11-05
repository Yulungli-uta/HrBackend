using System.Globalization;
using System.Text;

namespace WsUtaSystem.Application.Common;

public enum NameMatch
{
    /// <summary>Comparten al menos un nombre y un apellido.</summary>
    Match,
    /// <summary>Hay nombre en ambos lados y no coinciden.</summary>
    Mismatch,
    /// <summary>Uno de los lados no tiene nombre (p. ej. un ORCID que no publica su nombre).</summary>
    Unknown,
}

/// <summary>
/// Compara el nombre registrado en un servicio externo (ORCID) con el de la persona en RR.HH. Ignora tildes, mayúsculas, el
/// orden y partículas ("de", "del", "la"). Dos nombres coinciden si comparten al menos un nombre de pila y un apellido, de modo
/// que una persona registrada con dos nombres y dos apellidos coincide con quien usa solo uno de cada uno.
/// </summary>
public static class PersonNameMatcher
{
    private static readonly HashSet<string> Particles = new(StringComparer.Ordinal) { "de", "del", "la", "las", "los", "y", "da", "di", "van", "von" };

    public static NameMatch Compare(string? externalGivenNames, string? externalFamilyName, string? firstName, string? lastName)
    {
        var extGiven = Tokens(externalGivenNames);
        var extFamily = Tokens(externalFamilyName);
        var hrFirst = Tokens(firstName);
        var hrLast = Tokens(lastName);

        if ((extGiven.Count == 0 && extFamily.Count == 0) || (hrFirst.Count == 0 && hrLast.Count == 0))
            return NameMatch.Unknown;

        // Algunos perfiles escriben el nombre completo en un solo campo: se compara contra la unión de ambos lados.
        var extAll = extGiven.Union(extFamily).ToHashSet(StringComparer.Ordinal);
        var sharesFirst = hrFirst.Overlaps(extAll);
        var sharesLast = hrLast.Overlaps(extAll);
        return sharesFirst && sharesLast ? NameMatch.Match : NameMatch.Mismatch;
    }

    private static HashSet<string> Tokens(string? text)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text)) return set;

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetter(c) ? char.ToLowerInvariant(c) : ' ');
        }

        foreach (var token in sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (token.Length > 1 && !Particles.Contains(token)) set.Add(token);
        return set;
    }
}
