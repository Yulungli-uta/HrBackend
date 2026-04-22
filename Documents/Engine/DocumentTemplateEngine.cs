using System.Text.RegularExpressions;
using WsUtaSystem.Documents.Abstractions;

namespace WsUtaSystem.Documents.Engine;

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
            return resolvedValues.TryGetValue(token, out var value)
                ? System.Net.WebUtility.HtmlEncode(value)
                : string.Empty;
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
