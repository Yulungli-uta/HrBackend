namespace WsUtaSystem.Infrastructure.Configuration;

/// <summary>
/// Configuración de FileManagement desde appsettings.json
/// </summary>
public class FileManagementSettings
{
    /// <summary>
    /// Clave de encriptación AES-256 (32 caracteres)
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Credenciales de red para acceso a NAS/SMB (valores encriptados)
    /// </summary>
    public NetworkCredentials NetworkCredentials { get; set; } = new();
}

