namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte de novedades de asistencia. Una fila por
/// (jornada, tipo de novedad) — una misma jornada puede aparecer más de una vez si
/// tiene varias novedades a la vez (ej. atraso + ajuste manual). Cubre todo el
/// personal, no solo guardias.
/// </summary>
public sealed record AttendanceNoveltyReportDto
{
    public int EmployeeId { get; init; }
    public string IdCard { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public DateOnly WorkDate { get; init; }
    public int JourneyNumber { get; init; }

    /// <summary>Código estable del tipo de novedad (ej. "UNJUSTIFIED_ABSENCE"). Útil para filtrar/agrupar en el frontend.</summary>
    public string NoveltyType { get; init; } = string.Empty;

    /// <summary>Etiqueta corta del tipo (ej. "Ausencia injustificada").</summary>
    public string NoveltyLabel { get; init; } = string.Empty;

    /// <summary>Texto estándar de observación, con los valores concretos de esa jornada ya interpolados.</summary>
    public string Observation { get; init; } = string.Empty;

    public TimeOnly? ScheduledEntryTime { get; init; }
    public TimeOnly? ScheduledExitTime { get; init; }
}
