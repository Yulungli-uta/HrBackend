namespace WsUtaSystem.Infrastructure.ExternalServices.Common;

/// <summary>
/// Política anti-SSRF para servicios de terceros cuya URL se administra en Parámetros del Sistema: el adaptador envía su
/// credencial a esa URL, así que solo se acepta HTTPS, puerto por defecto, sin usuario/contraseña y un host exacto fijado
/// en el código del adaptador (no editable por quien administra los parámetros).
/// </summary>
public static class ExactHostUrlPolicy
{
    public static bool IsAllowed(Uri uri, string allowedHost) =>
        uri.Scheme == Uri.UriSchemeHttps
        && uri.HostNameType == UriHostNameType.Dns
        && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo)
        && uri.Host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase);
}
