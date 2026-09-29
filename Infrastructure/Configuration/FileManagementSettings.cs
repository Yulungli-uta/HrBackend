namespace WsUtaSystem.Infrastructure.Configuration;

/// <summary>
/// Configuración de FileManagement desde appsettings.json
/// </summary>
public class FileManagementSettings
{
    /// <summary>
    /// Indica si se debe usar Windows Impersonation para operaciones de archivos.
    /// true = Usar credenciales (NAS remoto), false = Acceso directo (punto de montaje local)
    /// </summary>
    public bool UseImpersonation { get; set; } = false;

    /// <summary>
    /// Cuando se configura (solo en appsettings.Development.json), reemplaza la raíz de unidad
    /// (ej. "G:\") de HR.tbl_DirectoryParameters.PhysicalPath por esta carpeta local, preservando
    /// el resto de la ruta relativa por DirectoryCode. Permite trabajar en local sin depender de
    /// que el NAS de producción (G:\ArchUta) esté accesible desde el equipo del desarrollador.
    /// En Production se deja sin configurar y no cambia nada del comportamiento actual.
    /// </summary>
    public string? LocalPhysicalPathOverride { get; set; }

    /// <summary>
    /// Clave de encriptación AES-256 (32 caracteres)
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Credenciales de red para acceso a NAS/SMB (valores encriptados)
    /// </summary>
    public NetworkCredentials NetworkCredentials { get; set; } = new();
}

