using System.Text.RegularExpressions;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Reports.Engine;

/// <summary>
/// Motor de sustitución de placeholders en plantillas HTML.
/// Reconoce tokens de la forma <c>{{FIELD_NAME}}</c> (case-insensitive).
/// Si un token no tiene valor resuelto, se reemplaza por cadena vacía
/// para evitar que el documento final muestre tokens sin resolver.
/// </summary>
public sealed class DocumentTemplateEngine : IDocumentTemplateEngine
{
    /// <summary>
    /// Patrón que reconoce <c>{{NOMBRE_CAMPO}}</c> con espacios opcionales.
    /// Ejemplo: <c>{{EMPLOYEE_FULLNAME}}</c>, <c>{{ CONTRACT_CODE }}</c>.
    /// </summary>
    private static readonly Regex TokenPattern = new(
        @"\{\{\s*([A-Za-z0-9_]+)\s*\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public string Render(string htmlContent, Dictionary<string, string> resolvedValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);
        ArgumentNullException.ThrowIfNull(resolvedValues);

        return TokenPattern.Replace(htmlContent, match =>
        {
            var token = match.Groups[1].Value.ToUpperInvariant();
            if (!resolvedValues.TryGetValue(token, out var value))
                return string.Empty;

            // Convención existente (ya usada por DISTRIBUTIVO_TABLE_HTML/HORARIO_TABLE_HTML,
            // nunca antes implementada): un campo cuyo nombre termina en _HTML contiene
            // marcado HTML de confianza construido por el propio backend (nunca entrada de
            // usuario directa) -- se inserta tal cual, sin HtmlEncode, para que
            // InstitutionalDocumentRenderer pueda parsear tablas/estructuras dentro de él.
            // Cualquier otro campo se sigue codificando como texto plano (comportamiento
            // sin cambios).
            return token.EndsWith("_HTML", StringComparison.Ordinal)
                ? value
                : System.Net.WebUtility.HtmlEncode(value);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ExtractTokens(string htmlContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);

        return TokenPattern
            .Matches(htmlContent)
            .Select(m => m.Groups[1].Value.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}
