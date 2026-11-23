namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Información de un logo configurable (sin el contenido binario).</summary>
public sealed record BrandAssetInfoDto(string Key, string Label, bool IsCustom, string? FileName, string? ContentType, string? Version, DateTime? UpdatedAt);

/// <summary>Contenido binario de un logo configurable.</summary>
public sealed record BrandAssetContent(byte[] Content, string ContentType, string Version);

/// <summary>Versión de cada logo del login (null = usar el logo oficial incluido en la aplicación).</summary>
public sealed record PublicBrandingDto(string? LoginLogoLight, string? LoginLogoDark);

/// <summary>
/// Logos parametrizables del sistema: login (claro y oscuro) y reportes. Sin configuración propia se usa el escudo oficial.
/// Los archivos se guardan en la BD para que sobrevivan a los despliegues.
/// </summary>
public interface IBrandingService
{
    /// <summary>Claves válidas: LOGIN_LOGO_LIGHT, LOGIN_LOGO_DARK, REPORT_LOGO.</summary>
    Task<IReadOnlyList<BrandAssetInfoDto>> ListAsync(CancellationToken ct);

    /// <summary>Contenido de un logo propio, o null si no hay (se usa el oficial).</summary>
    Task<BrandAssetContent?> GetContentAsync(string key, CancellationToken ct);

    /// <summary>Versiones de los logos del login para la pantalla pública de inicio de sesión.</summary>
    Task<PublicBrandingDto> GetPublicAsync(CancellationToken ct);

    /// <summary>Guarda o reemplaza un logo tras validar tipo, tamaño y contenido.</summary>
    Task<BrandAssetInfoDto> SaveAsync(string key, string fileName, string contentType, byte[] content, int? userId, CancellationToken ct);

    /// <summary>Quita el logo propio y vuelve al oficial.</summary>
    Task DeleteAsync(string key, CancellationToken ct);

    /// <summary>Escribe en App_Data/branding el logo de reportes guardado en BD (los PDF lo leen de disco). Se ejecuta al iniciar.</summary>
    Task MaterializeReportLogoAsync(CancellationToken ct);
}
