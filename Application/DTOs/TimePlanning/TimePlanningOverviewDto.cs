namespace WsUtaSystem.Application.DTOs.TimePlanning;

/// <summary>Filtros y paginación de la consulta de planificaciones (vista de Recursos Humanos).</summary>
public sealed record TimePlanningOverviewFilter(
    int Page = 1,
    int PageSize = 20,
    string? PlanType = "Overtime",
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? PlanStatusTypeId = null,
    int? EmployeeStatusTypeId = null,
    int? DepartmentId = null,
    string? Search = null,
    IReadOnlyList<int>? AllowedDepartmentIds = null);

/// <summary>Planificación con sus totales, para el listado paginado.</summary>
public sealed record TimePlanningOverviewItemDto(
    int PlanId,
    string PlanType,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? OvertimeType,
    decimal? Factor,
    int PlanStatusTypeId,
    string PlanStatusName,
    string CreatedByName,
    DateTime? CreatedAt,
    int TotalEmployees,
    decimal TotalAssignedHours,
    decimal TotalActualHours);

/// <summary>Empleado incluido en una planificación, con su estado y horas.</summary>
public sealed record TimePlanningOverviewEmployeeDto(
    int PlanEmployeeId,
    int PlanId,
    int EmployeeId,
    string EmployeeName,
    string IdCard,
    string DepartmentName,
    decimal? AssignedHours,
    decimal? ActualHours,
    int EmployeeStatusTypeId,
    string EmployeeStatusName,
    bool IsEligible,
    int ExecutionCount,
    int ExecutedOvertimeMinutes);
