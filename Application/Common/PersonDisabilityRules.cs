using System.Globalization;
using System.Text;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Reglas de los datos de discapacidad de una persona. Con tipo de discapacidad, el porcentaje (mayor que 0 y hasta 100) y el carnet
/// CONADIS son obligatorios y el tipo no puede ser «Ninguna»; sin tipo, el porcentaje y el carnet deben estar vacíos.
/// El formulario de HrFrontend aplica la misma regla; aquí se garantiza para cualquier otra vía de escritura.
/// </summary>
public static class PersonDisabilityRules
{
    /// <summary>Texto sin tildes ni mayúsculas, para comparar tipos del catálogo con datos escritos a mano.</summary>
    public static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().ToLowerInvariant();
    }

    /// <summary>«Ninguna» es el tipo del catálogo para "sin discapacidad": no es un tipo válido cuando la persona sí la tiene.</summary>
    public static bool IsNoneType(string? type) => NormalizeText(type) == "ninguna";

    /// <summary>
    /// Valida los datos y devuelve su forma normalizada (sin discapacidad: los tres vacíos; con discapacidad: valores recortados).
    /// Lanza <see cref="BusinessRuleException"/> con un mensaje para el usuario si no cumplen la regla.
    /// </summary>
    public static (string? Type, decimal? Percentage, string? Card) ValidateAndNormalize(string? type, decimal? percentage, string? card)
    {
        var cleanType = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        var cleanCard = string.IsNullOrWhiteSpace(card) ? null : card.Trim();

        if (cleanType is null)
        {
            if (percentage is > 0)
                throw new BusinessRuleException("No se puede registrar un porcentaje de discapacidad si la persona no tiene un tipo de discapacidad.");
            if (cleanCard is not null)
                throw new BusinessRuleException("No se puede registrar un carnet CONADIS si la persona no tiene un tipo de discapacidad.");
            return (null, null, null);
        }

        if (IsNoneType(cleanType))
            throw new BusinessRuleException("El tipo de discapacidad no puede ser «Ninguna»: deje el tipo vacío si la persona no tiene discapacidad.");
        if (percentage is null or <= 0)
            throw new BusinessRuleException("El porcentaje de discapacidad es obligatorio y debe ser mayor que 0 cuando la persona tiene discapacidad.");
        if (percentage > 100)
            throw new BusinessRuleException("El porcentaje de discapacidad no puede ser mayor a 100.");
        if (cleanCard is null)
            throw new BusinessRuleException("El carnet CONADIS es obligatorio cuando la persona tiene discapacidad.");

        return (cleanType, percentage, cleanCard);
    }

    /// <summary>
    /// true si los datos de discapacidad cambiaron. Vacío y 0 en el porcentaje se consideran lo mismo, y el tipo se compara sin tildes ni mayúsculas.
    /// Sirve para no exigir la regla a quien guarda otros datos de una persona cuyos datos de discapacidad anteriores ya eran incompletos.
    /// </summary>
    public static bool HasChanged(
        string? oldType, decimal? oldPercentage, string? oldCard,
        string? newType, decimal? newPercentage, string? newCard) =>
        NormalizeText(oldType) != NormalizeText(newType)
        || (oldPercentage ?? 0m) != (newPercentage ?? 0m)
        || !string.Equals(oldCard?.Trim() ?? string.Empty, newCard?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
}
