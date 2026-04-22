namespace WsUtaSystem.Documents.Abstractions;

/// <summary>
/// Convierte el HTML final (con placeholders ya sustituidos) en un archivo PDF.
/// </summary>
public interface IDocumentRenderer
{
    /// <summary>
    /// Renderiza el HTML a bytes PDF.
    /// </summary>
    /// <param name="htmlContent">HTML completo con estilos CSS incrustados.</param>
    /// <param name="cssStyles">CSS adicional a aplicar (puede ser null).</param>
    /// <returns>Array de bytes del PDF generado.</returns>
    Task<byte[]> RenderToPdfAsync(string htmlContent, string? cssStyles = null);
}
