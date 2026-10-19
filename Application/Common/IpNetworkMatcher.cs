using System.Net;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Utilidades para listas de IP/rangos permitidos: interpreta "190.10.20.5" o "190.10.20.0/24"
/// (IPv4 o IPv6) y comprueba pertenencia. Una IPv6 mapeada de IPv4 (::ffff:a.b.c.d) se trata como IPv4.
/// </summary>
public static class IpNetworkMatcher
{
    /// <summary>Normaliza una IPv4-mapeada-en-IPv6 a IPv4 para poder compararla con rangos IPv4.</summary>
    public static IPAddress Normalize(IPAddress ip) => ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;

    /// <summary>
    /// Interpreta una entrada de la lista. Devuelve false con un mensaje en español si no es válida.
    /// <paramref name="normalized"/> es el texto canónico que se guarda (sin espacios, IP sola o CIDR).
    /// </summary>
    public static bool TryParseEntry(string? text, out IPNetwork network, out string normalized, out string error)
    {
        network = default;
        normalized = string.Empty;
        error = string.Empty;

        var value = (text ?? string.Empty).Trim();
        if (value.Length == 0) { error = "Ingrese una IP o un rango."; return false; }
        if (value.Length > 64) { error = "El valor es demasiado largo."; return false; }

        if (value.Contains('/'))
        {
            var parts = value.Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var baseIp) || !int.TryParse(parts[1], out var prefix))
            { error = "El rango debe tener el formato 190.10.20.0/24."; return false; }

            baseIp = Normalize(baseIp);
            var maxPrefix = baseIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
            if (prefix < 0 || prefix > maxPrefix) { error = "El tamaño del rango (después de la /) no es válido."; return false; }
            // Un prefijo demasiado amplio dejaría pasar casi cualquier IP; se exige un mínimo razonable.
            var minPrefix = maxPrefix == 32 ? 8 : 32;
            if (prefix < minPrefix) { error = $"El rango es demasiado amplio (mínimo /{minPrefix})."; return false; }

            try { network = new IPNetwork(baseIp, prefix); }
            catch (ArgumentException) { error = "El rango no es válido: la dirección base debe ser la primera del rango."; return false; }

            normalized = prefix == maxPrefix ? baseIp.ToString() : $"{network.BaseAddress}/{network.PrefixLength}";
            return true;
        }

        if (!IPAddress.TryParse(value, out var ip)) { error = "La IP no es válida."; return false; }
        ip = Normalize(ip);
        network = new IPNetwork(ip, ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128);
        normalized = ip.ToString();
        return true;
    }

    /// <summary>¿La IP pertenece a alguno de los rangos?</summary>
    public static bool Contains(IEnumerable<IPNetwork> networks, IPAddress ip)
    {
        var candidate = Normalize(ip);
        foreach (var n in networks)
        {
            if (n.BaseAddress.AddressFamily == candidate.AddressFamily && n.Contains(candidate)) return true;
        }
        return false;
    }
}
