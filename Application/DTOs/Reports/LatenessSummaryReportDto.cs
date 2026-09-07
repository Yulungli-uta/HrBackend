namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// Conteo de atrasos por empleado dentro de un rango de fechas — una fila por empleado,
/// a diferencia de <see cref="LatenessReportDto"/> que trae una fila por cada día con atraso.
/// </summary>
public sealed record LatenessSummaryReportDto
{
    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Número de cédula del empleado.</summary>
    public string IdCard { get; init; } = string.Empty;

    /// <summary>Nombre completo del empleado (LastName + FirstName).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Nombre del departamento/dependencia del empleado.</summary>
    public string? DepartmentName { get; init; }

    /// <summary>Régimen laboral / tipo de contrato del empleado (texto legible).</summary>
    public string? ContractType { get; init; }

    /// <summary>Cantidad de días con atraso (MinutesLate &gt; 0 o TardinessMin &gt; 0) en el rango.</summary>
    public int LateDaysCount { get; init; }

    /// <summary>Suma de minutos de atraso (TardinessMin) en el rango.</summary>
    public int TotalMinutesLate { get; init; }
}
