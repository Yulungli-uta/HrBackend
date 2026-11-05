namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte consolidado de horas nocturnas. Una fila
/// por empleado: suma de <c>HR.tbl_AttendanceCalculations.NightMinutes</c> en
/// el período, según la ventana nocturna parametrizada en
/// <c>HR.tbl_Parameters</c> (<c>NIGHT_START</c> / <c>NIGHT_END</c>).
/// </summary>
public sealed record NightHoursSummaryReportDto
{
    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Número de cédula del empleado.</summary>
    public string IdCard { get; init; } = string.Empty;

    /// <summary>Nombre completo del empleado (LastName + FirstName).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Dependencia del empleado.</summary>
    public string? DepartmentName { get; init; }

    /// <summary>Tipo de contrato del empleado.</summary>
    public string? ContractType { get; init; }

    /// <summary>Cantidad de jornadas (días) con al menos un minuto nocturno en el período.</summary>
    public int DaysWithNightHours { get; init; }

    /// <summary>Total de minutos nocturnos trabajados en el período.</summary>
    public int TotalNightMinutes { get; init; }

    /// <summary>Total de horas nocturnas trabajadas en el período (TotalNightMinutes / 60).</summary>
    public decimal TotalNightHours { get; init; }

    /// <summary>
    /// Horarios (entrada y salida) realmente aplicados en las jornadas con horas nocturnas del período,
    /// sin repetir y ordenados por hora de entrada.
    /// </summary>
    public IReadOnlyList<NightHoursScheduleRange> AssignedSchedules { get; init; } = [];
}

/// <summary>Horario aplicado en una jornada: hora de entrada y de salida.</summary>
public sealed record NightHoursScheduleRange(TimeOnly Entry, TimeOnly Exit);

/// <summary>
/// Resultado del reporte de horas nocturnas: las filas mostradas y, cuando se excluyen guardias,
/// cuánto se dejó fuera, para que el reporte lo declare y los totales se puedan cuadrar.
/// </summary>
/// <param name="Rows">Una fila por empleado incluido.</param>
/// <param name="ExcludedJobNames">Cargos excluidos (vacío si no se excluyó a nadie).</param>
/// <param name="ExcludedPeople">Personas excluidas que tenían horas nocturnas en el período.</param>
/// <param name="ExcludedHours">Horas nocturnas de las personas excluidas.</param>
public sealed record NightHoursSummaryResultDto(
    IReadOnlyList<NightHoursSummaryReportDto> Rows,
    IReadOnlyList<string> ExcludedJobNames,
    int ExcludedPeople,
    decimal ExcludedHours);
