namespace WsUtaSystem.Application.Common;

/// <summary>
/// Enmascara una identificación (cédula o pasaporte) para dejarla en registros de auditoría sin exponerla completa.
/// </summary>
public static class IdentificationMasker
{
    private const int VisibleStart = 3;
    private const int VisibleEnd = 2;
    private const char MaskChar = '*';

    /// <summary>
    /// Conserva los 3 primeros y los 2 últimos caracteres y reemplaza el resto por asteriscos (1715559439 → 171*****39).
    /// Si la identificación es demasiado corta para ocultar algo sin dejarla completa, se enmascara por entero.
    /// </summary>
    public static string Mask(string? identification)
    {
        var value = identification?.Trim() ?? string.Empty;
        if (value.Length == 0) return string.Empty;
        if (value.Length <= VisibleStart + VisibleEnd) return new string(MaskChar, value.Length);

        return string.Concat(
            value.AsSpan(0, VisibleStart),
            new string(MaskChar, value.Length - VisibleStart - VisibleEnd),
            value.AsSpan(value.Length - VisibleEnd));
    }
}
