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
}
