namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte consolidado de subsidio de alimentación.
/// Una fila por empleado: suma de días efectivamente laborados en el período
/// (<c>HR.tbl_AttendanceCalculations.FoodSubsidy = 1</c>) multiplicada por el
/// valor diario parametrizado en <c>HR.tbl_Parameters</c> (<c>FOOD_SUBSIDY_DAILY_VALUE</c>).
/// </summary>
public sealed record FoodSubsidySummaryReportDto
{
    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Número de cédula del empleado.</summary>
    public string IdCard { get; init; } = string.Empty;

    /// <summary>Nombre completo del empleado (LastName + FirstName).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Dependencia del empleado.</summary>
    public string? DepartmentName { get; init; }

    /// <summary>Tipo de contrato del empleado (ej. "Código Trabajo").</summary>
    public string? ContractType { get; init; }

    /// <summary>Días efectivamente laborados en el período (suma de FoodSubsidy = 1).</summary>
    public int DaysWorked { get; init; }

    /// <summary>Valor diario del subsidio (parámetro FOOD_SUBSIDY_DAILY_VALUE).</summary>
    public decimal UnitValue { get; init; }

    /// <summary>Total del subsidio en el período (DaysWorked * UnitValue).</summary>
    public decimal TotalValue { get; init; }
}
