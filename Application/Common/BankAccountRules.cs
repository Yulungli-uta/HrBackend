using System.Globalization;
using System.Text;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Reglas para detectar cuentas bancarias duplicadas. Dos cuentas son la misma si coinciden la entidad financiera
/// (sin importar mayúsculas, tildes ni espacios sobrantes) y el número de cuenta (sin espacios, guiones ni puntos).
/// </summary>
public static class BankAccountRules
{
    /// <summary>Entidad financiera sin tildes, en minúsculas y con los espacios normalizados.</summary>
    public static string NormalizeInstitution(string? institution)
    {
        if (string.IsNullOrWhiteSpace(institution)) return string.Empty;
        var decomposed = institution.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace) sb.Append(' ');
                lastWasSpace = true;
                continue;
            }
            lastWasSpace = false;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Número de cuenta solo con letras y dígitos, en mayúsculas (se ignoran espacios, guiones y puntos).</summary>
    public static string NormalizeNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return string.Empty;
        var sb = new StringBuilder(accountNumber.Length);
        foreach (var c in accountNumber)
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        return sb.ToString();
    }

    /// <summary>true si ambas cuentas son la misma (misma entidad y mismo número). Un número vacío nunca cuenta como duplicado.</summary>
    public static bool IsSameAccount(string? institutionA, string? numberA, string? institutionB, string? numberB)
    {
        var number = NormalizeNumber(numberA);
        return number.Length > 0
            && number == NormalizeNumber(numberB)
            && NormalizeInstitution(institutionA) == NormalizeInstitution(institutionB);
    }
}
