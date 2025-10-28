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
    /// Clave de encriptación AES-256 (32 caracteres)
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Credenciales de red para acceso a NAS/SMB (valores encriptados)
    /// </summary>
    public NetworkCredentials NetworkCredentials { get; set; } = new();
}

