namespace WsUtaSystem.Documents.Abstractions;

/// <summary>
/// Motor de sustitución de placeholders en plantillas HTML.
/// Reemplaza tokens del tipo <c>{{FIELD_NAME}}</c> con los valores resueltos.
/// </summary>
public interface IDocumentTemplateEngine
{
    /// <summary>
    /// Aplica los valores resueltos al contenido HTML de la plantilla,
    /// sustituyendo todos los tokens <c>{{FIELD_NAME}}</c>.
    /// </summary>
    /// <param name="htmlContent">Contenido HTML de la plantilla con placeholders.</param>
    /// <param name="resolvedValues">Diccionario de fieldName → valor resuelto.</param>
    /// <returns>HTML final con todos los placeholders sustituidos.</returns>
    string Render(string htmlContent, Dictionary<string, string> resolvedValues);

    /// <summary>
    /// Extrae todos los tokens <c>{{FIELD_NAME}}</c> presentes en el HTML.
    /// Útil para validar que todos los campos de la plantilla están definidos.
    /// </summary>
    IReadOnlyList<string> ExtractTokens(string htmlContent);
}
