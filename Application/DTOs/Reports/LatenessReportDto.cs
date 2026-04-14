namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte de atrasos.
/// Los datos se obtienen directamente de <c>HR.tbl_AttendanceCalculations</c>
/// mediante una consulta EF Core + LINQ con join a <c>Employees</c> y <c>People</c>.
/// </summary>
/// <remarks>
/// Principio SRP: este record solo transporta los datos proyectados desde la capa
/// de repositorio hacia la capa de presentación (IReportSource).
/// No contiene lógica de negocio.
/// </remarks>
public sealed record LatenessReportDto
{
    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Número de cédula del empleado.</summary>
    public string IdCard { get; init; } = string.Empty;

    /// <summary>Nombre completo del empleado (FirstName + LastName).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Fecha de trabajo.</summary>
    public DateOnly WorkDate { get; init; }

    /// <summary>
    /// Minutos de atraso bruto (diferencia entre entrada real y hora de entrada del horario).
    /// Incluye los minutos de gracia.
    /// </summary>
    public int MinutesLate { get; init; }

    /// <summary>
    /// Minutos de tardanza neta (atraso bruto menos minutos de gracia configurados).
    /// Es el valor que se descuenta al empleado.
    /// </summary>
    public int TardinessMin { get; init; }

    /// <summary>Hora de entrada del horario programado.</summary>
    public TimeOnly? ScheduledEntryTime { get; init; }

    /// <summary>Hora real de primera marcación de entrada.</summary>
    public DateTime? FirstPunchIn { get; init; }

    /// <summary>Minutos de salida anticipada del turno.</summary>
    public int EarlyLeaveMinutes { get; init; }

    /// <summary>Estado del cálculo (p.ej. PROCESSED, ABSENT, HOLIDAY).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Indica si el empleado tiene justificación aprobada para este día.</summary>
    public bool HasJustification { get; init; }

    /// <summary>Minutos justificados que compensan el atraso.</summary>
    public int JustificationMinutes { get; init; }

    /// <summary>Versión del cálculo (útil para auditoría).</summary>
    public int CalculationVersion { get; init; }
}
