namespace WsUtaSystem.Application.Common;

/// <summary>
/// Reglas de la ventana de tiempo de un permiso. Deben coincidir con las de
/// <c>HR.sp_ProcessAttendanceLeavesDay</c> y <c>HR.vw_LeaveWindows</c> (el cálculo de asistencia).
/// </summary>
/// <remarks>
/// El formulario guarda un permiso de día completo (o de varios días) solo con fechas: Inicio y Fin a las 00:00.
/// Igual que en las vacaciones, el Fin de ese permiso es INCLUSIVO: la ventana llega al final de ese día.
/// Un <c>HourTaken</c> entre 1 y (jornada - 1) con fechas sin hora es un permiso por horas guardado sin
/// horario; no se interpreta como día completo.
/// </remarks>
public static class PermissionWindowRules
{
    /// <summary>Inicio y Fin vienen exactamente a las 00:00 (solo fechas).</summary>
    public static bool IsDateOnly(DateTime start, DateTime end) =>
        start.TimeOfDay == TimeSpan.Zero && end.TimeOfDay == TimeSpan.Zero;

    /// <summary>
    /// Permiso de día completo o de varios días: solo fechas y <paramref name="hourTaken"/> nulo, 0 o
    /// una jornada completa (<paramref name="minutesPerDay"/>) o más.
    /// </summary>
    public static bool IsFullDay(DateTime start, DateTime end, decimal? hourTaken, int minutesPerDay) =>
        IsDateOnly(start, end) && (hourTaken is null || hourTaken <= 0 || hourTaken >= minutesPerDay);

    /// <summary>
    /// Fin efectivo (inclusivo) del permiso para comparar rangos: el último segundo del día cuando es de día
    /// completo; el Fin guardado en cualquier otro caso.
    /// </summary>
    public static DateTime EffectiveEndInclusive(DateTime start, DateTime end, decimal? hourTaken, int minutesPerDay) =>
        IsFullDay(start, end, hourTaken, minutesPerDay) ? end.Date.AddDays(1).AddSeconds(-1) : end;

    /// <summary>
    /// Minutos del permiso cuando la solicitud no los trae (nulo o 0): días laborables x jornada si son solo
    /// fechas, o la diferencia entre Inicio y Fin si trae horario. Si ya trae un valor positivo, se conserva.
    /// </summary>
    public static decimal ResolveHourTaken(
        DateTime start, DateTime end, decimal? hourTaken, int minutesPerDay, Func<DateTime, DateTime, int> countWorkingDays)
    {
        ArgumentNullException.ThrowIfNull(countWorkingDays);
        if (hourTaken is > 0) return hourTaken.Value;

        if (IsDateOnly(start, end))
            return countWorkingDays(start.Date, end.Date) * (decimal)minutesPerDay;

        var minutes = (end - start).TotalMinutes;
        return minutes > 0 ? (decimal)Math.Round(minutes, 2) : 0m;
    }
}
