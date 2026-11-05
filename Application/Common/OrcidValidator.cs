using System.Text.RegularExpressions;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Normalización y validación del ORCID iD (formato <c>0000-0000-0000-000X</c>, dígito verificador ISO 7064 MOD 11-2).
/// </summary>
public static partial class OrcidValidator
{
    [GeneratedRegex(@"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$")]
    private static partial Regex CanonicalFormat();

    /// <summary>
    /// Devuelve el ORCID en formato canónico (acepta la URL <c>https://orcid.org/…</c>, sin guiones y en minúscula) o
    /// <c>null</c> si no tiene 16 caracteres válidos. No comprueba el dígito verificador: ver <see cref="IsValid"/>.
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var value = input.Trim();
        value = Regex.Replace(value, @"^(https?://)?(www\.)?orcid\.org/", string.Empty, RegexOptions.IgnoreCase);
        value = value.Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        if (value.Length != 16 || !value.Take(15).All(char.IsAsciiDigit) || !(char.IsAsciiDigit(value[15]) || value[15] == 'X'))
            return null;

        return $"{value[..4]}-{value[4..8]}-{value[8..12]}-{value[12..]}";
    }

    /// <summary>Comprueba el formato canónico y el dígito verificador (ISO 7064 MOD 11-2).</summary>
    public static bool IsValid(string? canonical)
    {
        if (canonical is null || !CanonicalFormat().IsMatch(canonical)) return false;

        var digits = canonical.Replace("-", string.Empty);
        var total = 0;
        for (var i = 0; i < 15; i++) total = (total + (digits[i] - '0')) * 2;
        var result = (12 - total % 11) % 11;
        var expected = result == 10 ? 'X' : (char)('0' + result);
        return digits[15] == expected;
    }
}
