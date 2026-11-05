namespace WsUtaSystem.Infrastructure.ExternalServices.Common;

/// <summary>
/// Política anti-SSRF para URLs de servicios externos que se administran en HR.tbl_Parameters: cualquiera con
/// acceso a Parámetros podría editarlas, así que solo se aceptan hosts institucionales.
/// </summary>
public static class InstitutionalUrlPolicy
{
    private const string AllowedHostSuffix = "uta.edu.ec";

    /// <summary>
    /// HTTPS, nombre DNS (no IP), puerto por defecto, sin usuario/contraseña y dominio uta.edu.ec o subdominio.
    /// Con <paramref name="allowLoopback"/> (solo Development) también se acepta localhost por HTTP/HTTPS.
    /// </summary>
    public static bool IsAllowed(Uri uri, bool allowLoopback = false)
    {
        if (!string.IsNullOrEmpty(uri.UserInfo)) return false;

        if (allowLoopback && uri.IsLoopback
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return true;

        return uri.Scheme == Uri.UriSchemeHttps
            && uri.HostNameType == UriHostNameType.Dns
            && uri.IsDefaultPort
            && (uri.Host.Equals(AllowedHostSuffix, StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith("." + AllowedHostSuffix, StringComparison.OrdinalIgnoreCase));
    }
}
