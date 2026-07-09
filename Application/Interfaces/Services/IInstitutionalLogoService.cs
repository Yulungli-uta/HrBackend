namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Provee acceso centralizado al logo institucional (único archivo en wwwroot del backend),
/// para que reportes (QuestPDF) y plantillas documentales (HTML/Puppeteer) lo consuman desde
/// una sola fuente de verdad en lugar de duplicar la lectura del archivo en cada renderer.
/// </summary>
public interface IInstitutionalLogoService
{
    /// <summary>Ruta absoluta en disco del logo, o null si el archivo configurado no existe.</summary>
    string? GetLogoFilePath();

    /// <summary>Logo como data URI base64 (data:image/png;base64,...), o cadena vacía si no existe.</summary>
    string GetLogoDataUri();
}
