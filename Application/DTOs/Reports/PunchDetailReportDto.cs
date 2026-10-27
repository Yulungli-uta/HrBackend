namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// Una fila del reporte detallado de picadas: una jornada de un empleado con su horario programado,
/// las cuatro picadas (entrada, salida a almuerzo, regreso de almuerzo, salida) y si tuvo atraso.
/// Incluye también los días programados sin ninguna picada (ausencias).
/// </summary>
public sealed record PunchDetailReportDto
{
    public int EmployeeId { get; init; }
    public string IdCard { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public DateOnly WorkDate { get; init; }
    public int JourneyNumber { get; init; }

    public TimeOnly? ScheduledEntryTime { get; init; }
    public TimeOnly? ScheduledExitTime { get; init; }
    /// <summary>El horario programado tiene hora de almuerzo.</summary>
    public bool HasLunch { get; init; }
    public TimeOnly? ScheduledLunchStart { get; init; }
    public TimeOnly? ScheduledLunchEnd { get; init; }

    public DateTime? EntryPunch { get; init; }
    public DateTime? LunchOutPunch { get; init; }
    public DateTime? LunchInPunch { get; init; }
    public DateTime? ExitPunch { get; init; }
    /// <summary>Total de picadas de la jornada (puede ser mayor a 4 si hay picadas repetidas).</summary>
    public int PunchCount { get; init; }

    /// <summary>Minutos de atraso del día (entrada tardía o regreso tardío del almuerzo).</summary>
    public int LateMinutes { get; init; }
    public bool HasLateness { get; init; }
}
