namespace WsUtaSystem.Application.Common;

/// <summary>
/// Intervalo mínimo entre picadas de un mismo empleado. La regla existía solo en el trigger
/// HR.trg_Punch_Validations, que está deshabilitado (y no debe habilitarse: bloquearía la importación de biométricos),
/// por lo que ninguna capa la aplicaba. Ahora la aplica el backend al registrar la propia picada desde la web.
/// </summary>
public static class PunchCooldown
{
    public const int DefaultMinutes = 5;
    public const int MaxMinutes = 120;

    /// <summary>
    /// Tiempo que falta para poder picar de nuevo, o null si ya se puede. Con <paramref name="minMinutes"/> ≤ 0 la regla
    /// está desactivada. Se usa la diferencia absoluta: una picada "futura" (reloj de un dispositivo desfasado) también
    /// cuenta como reciente.
    /// </summary>
    public static TimeSpan? Remaining(DateTime? lastPunch, DateTime now, int minMinutes)
    {
        if (minMinutes <= 0 || lastPunch is null) return null;
        var gap = (now - lastPunch.Value).Duration();
        var min = TimeSpan.FromMinutes(minMinutes);
        return gap < min ? min - gap : null;
    }

    /// <summary>Mensaje para el usuario con el tiempo que debe esperar.</summary>
    public static string Message(TimeSpan remaining)
    {
        var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
        var wait = remaining < TimeSpan.FromMinutes(1) ? "unos segundos" : minutes == 1 ? "1 minuto" : $"{minutes} minutos";
        return $"Ya registraste una picada hace poco. Espera {wait} para volver a picar.";
    }

    /// <summary>Valor del parámetro: entero entre 0 y <see cref="MaxMinutes"/>; vacío o inválido = valor por defecto.</summary>
    public static int ParseMinutes(string? raw) =>
        int.TryParse(raw?.Trim(), out var n) ? Math.Clamp(n, 0, MaxMinutes) : DefaultMinutes;
}
