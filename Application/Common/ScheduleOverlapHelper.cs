using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Compara dos horarios del MISMO día como rangos en minutos desde las 00:00 de ese día,
/// para saber si se encimarían o quedarían pegados sin descanso entre ellos (jornada
/// continua). Compartido entre RotationPatternService (validar un patrón de rotación) y
/// GuardAssignmentValidationService (validar una asignación/reasignación puntual) — antes
/// duplicado en el primero únicamente.
/// </summary>
public static class ScheduleOverlapHelper
{
    /// <summary>
    /// Un turno que cruza medianoche se representa extendido más allá de 1440
    /// (ej. 23:00-07:30 = [1380, 1890)). No se debe "envolver" la comparación a ±1440:
    /// ambos horarios arrancan el mismo día de calendario, así que comparar el rango tal
    /// cual ya es correcto — envolver generaba falsos positivos (ej. Mañana 07-15:30 vs
    /// Noche 23-07:30 detectados como encimados por comparar la cola de una Noche de OTRO
    /// día contra la Mañana de este día).
    /// </summary>
    public static bool AreBackToBackOrOverlapping(Schedules a, Schedules b)
    {
        var (aStart, aEnd) = ToMinuteRange(a);
        var (bStart, bEnd) = ToMinuteRange(b);

        if (bStart < aEnd && bEnd > aStart) return true;
        if (bStart == aEnd || bEnd == aStart) return true;

        return false;
    }

    public static (int start, int end) ToMinuteRange(Schedules s)
    {
        var start = s.EntryTime.Hour * 60 + s.EntryTime.Minute;
        var durationMinutes = ((s.ExitTime.Hour * 60 + s.ExitTime.Minute) - start + 1440) % 1440;
        if (durationMinutes == 0) durationMinutes = 1440;
        return (start, start + durationMinutes);
    }
}
