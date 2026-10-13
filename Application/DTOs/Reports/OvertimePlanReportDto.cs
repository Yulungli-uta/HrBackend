namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el detalle diario del reporte "Horas Extra: Planificado vs.
/// Ejecutado". Una fila por (PlanEmployeeID, WorkDate) — el mismo plan/empleado se repite
/// una vez por cada día con ejecución registrada en <c>HR.tbl_TimePlanningExecution</c>.
/// </summary>
public sealed record OvertimePlanDailyReportDto
{
    public int EmployeeId { get; init; }
    public string IdCard { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }

    public int PlanId { get; init; }
    public string PlanTitle { get; init; } = string.Empty;
    public DateTime PlanStartDate { get; init; }
    public DateTime PlanEndDate { get; init; }

    /// <summary>Cuota total planificada para todo el plan (no por día) — HR.tbl_TimePlanningEmployees.AssignedHours.</summary>
    public decimal? AssignedHours { get; init; }

    public DateOnly WorkDate { get; init; }
    public int ExecutedTotalMinutes { get; init; }
    public int ExecutedOvertimeMinutes { get; init; }
    public int ExecutedNightMinutes { get; init; }
    public int ExecutedHolidayMinutes { get; init; }

    /// <summary>Estado del empleado en el plan (EMPLOYEE_PLAN_STATUS: Asignado/En Proceso/Ejecutado/Cancelado).</summary>
    public string EmployeeStatusName { get; init; } = string.Empty;

    /// <summary>Estado de la fila consolidada en HR.tbl_Overtime para ese EmployeeID+WorkDate, si existe (EXECUTED/APPROVED/PAID).</summary>
    public string? OvertimeStatus { get; init; }
}

/// <summary>
/// DTO de proyección para el resumen mensual del reporte "Horas Extra: Planificado vs.
/// Ejecutado". Una fila por (EmployeeID, Año-Mes) — suma TODOS los planes de horas extra que
/// el empleado tuvo ese mes (no por plan individual, a diferencia de
/// <see cref="OvertimePlanDailyReportDto"/>), para que una persona con más de una
/// planificación en el mismo mes aparezca en una sola fila con el total correcto.
/// </summary>
public sealed record OvertimePlanMonthlyReportDto
{
    public int EmployeeId { get; init; }
    public string IdCard { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }

    /// <summary>Suma de las cuotas de TODOS los planes del empleado activos ese mes (cada plan se cuenta una sola vez, no por cada día ejecutado).</summary>
    public decimal AssignedHours { get; init; }

    /// <summary>Período calendario del agregado, formato "yyyy-MM".</summary>
    public string Period { get; init; } = string.Empty;

    /// <summary>Cantidad de planes distintos que el empleado tuvo ese mes.</summary>
    public int PlanCount { get; init; }

    public int ExecutedTotalMinutes { get; init; }
    public int ExecutedOvertimeMinutes { get; init; }
    public int ExecutedNightMinutes { get; init; }
    public int ExecutedHolidayMinutes { get; init; }
}
