namespace WsUtaSystem.Infrastructure.Configuration;

/// <summary>
/// Credenciales de red para acceso a NAS/SMB
/// Los valores deben estar encriptados en appsettings.json
/// </summary>
public class NetworkCredentials
{
    /// <summary>
    /// Nombre de usuario (encriptado en appsettings)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña (encriptada en appsettings)
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Dominio o nombre del servidor (encriptado en appsettings)
    /// </summary>
    public string? Domain { get; set; }
}

