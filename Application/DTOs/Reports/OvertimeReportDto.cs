namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte de horas extras.
/// Los datos se obtienen directamente de <c>HR.tbl_AttendanceCalculations</c>
/// mediante una consulta EF Core + LINQ con join a <c>Employees</c> y <c>People</c>.
/// </summary>
/// <remarks>
/// Incluye minutos de horas extras ordinarias, nocturnas, feriado y fuera de horario
/// para dar una visión completa del tiempo adicional trabajado por el empleado.
/// </remarks>
public sealed record OvertimeReportDto
{
    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Número de cédula del empleado.</summary>
    public string IdCard { get; init; } = string.Empty;

    /// <summary>Nombre completo del empleado (FirstName + LastName).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Fecha de trabajo.</summary>
    public DateOnly WorkDate { get; init; }

    /// <summary>Total de minutos trabajados en el día.</summary>
    public int TotalWorkedMinutes { get; init; }

    /// <summary>Minutos de horas extras ordinarias (fuera del horario regular).</summary>
    public int OvertimeMinutes { get; init; }

    /// <summary>Minutos trabajados en horario nocturno.</summary>
    public int NightMinutes { get; init; }

    /// <summary>Minutos trabajados en días feriados.</summary>
    public int HolidayMinutes { get; init; }

    /// <summary>
    /// Minutos trabajados fuera del horario programado (antes o después del turno).
    /// </summary>
    public int OffScheduleMin { get; init; }

    /// <summary>Minutos de horario regular trabajados.</summary>
    public int RegularMinutes { get; init; }

    /// <summary>Minutos de horario programado para ese día.</summary>
    public int ScheduledMinutes { get; init; }

    /// <summary>Hora de entrada del horario programado.</summary>
    public TimeOnly? ScheduledEntryTime { get; init; }

    /// <summary>Hora de salida del horario programado.</summary>
    public TimeOnly? ScheduledExitTime { get; init; }

    /// <summary>Hora real de primera marcación de entrada.</summary>
    public DateTime? FirstPunchIn { get; init; }

    /// <summary>Hora real de última marcación de salida.</summary>
    public DateTime? LastPunchOut { get; init; }

    /// <summary>Estado del cálculo (p.ej. PROCESSED, ABSENT, HOLIDAY).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Versión del cálculo (útil para auditoría).</summary>
    public int CalculationVersion { get; init; }
}
