namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte de subsidio de alimentación por empleado y
/// horario trabajado. Una fila por combinación (empleado, horario aplicado): suma de
/// jornadas que calificaron para el subsidio (<c>HR.tbl_AttendanceCalculations.FoodSubsidy = 1</c>)
/// en ese horario durante el rango de fechas, multiplicada por el valor diario
/// parametrizado en <c>HR.tbl_Parameters</c> (<c>FOOD_SUBSIDY_DAILY_VALUE</c>).
/// A diferencia de <see cref="FoodSubsidySummaryReportDto"/> (un solo total por empleado,
/// sin distinguir horario), este reporte separa por horario dentro de cada empleado —
/// útil para guardias con turno doble, que aparecen con una fila por cada horario distinto
/// que cumplieron (ej. una fila para el turno de mañana, otra para el de noche).
/// Ordenado por nombre de empleado.
/// </summary>
public sealed record FoodSubsidyByScheduleReportDto
{
    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Número de cédula del empleado.</summary>
    public string IdCard { get; init; } = string.Empty;

    /// <summary>Nombre completo del empleado (LastName + FirstName).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Hora de entrada del horario.</summary>
    public TimeOnly? EntryTime { get; init; }

    /// <summary>Hora de salida del horario.</summary>
    public TimeOnly? ExitTime { get; init; }

    /// <summary>Cantidad de jornadas que calificaron para el subsidio en este horario, en el período (suma de FoodSubsidy = 1).</summary>
    public int JourneysCount { get; init; }

    /// <summary>Valor diario del subsidio (parámetro FOOD_SUBSIDY_DAILY_VALUE).</summary>
    public decimal UnitValue { get; init; }

    /// <summary>Total del subsidio para este empleado en este horario (JourneysCount * UnitValue).</summary>
    public decimal TotalValue { get; init; }
}
