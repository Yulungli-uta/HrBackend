namespace WsUtaSystem.Application.DTOs.MassVacationPlan;

public class MassVacationPlanDto
{
    public int PlanId { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int VacationYear { get; set; }

    public int StatusTypeId { get; set; }
    /// <summary>Código de HR.ref_Types (Name), ej. "PLANNED" — usar para lógica/comparaciones.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Etiqueta en español de HR.ref_Types (Description), ej. "Planificado" — usar solo para mostrar.</summary>
    public string StatusLabel { get; set; } = string.Empty;

    public int TotalEmployeesInScope { get; set; }
    public int TotalExcluded { get; set; }
    public int? ExecutedBy { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class MassVacationPlanCreateDto
{
    /// <summary>NULL = toda la institución.</summary>
    public int? DepartmentId { get; set; }
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Modo "por horas": si se especifican, StartDate debe ser igual a EndDate.</summary>
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public int VacationYear { get; set; }
}

/// <summary>Una fila del roster: un empleado del alcance del plan, con su estado de exclusión actual.</summary>
public class MassVacationPlanRosterItemDto
{
    public int EmployeeId { get; set; }
    public string IdCard { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public bool IsExcluded { get; set; }
    public string? ExclusionReason { get; set; }
}

public class MassVacationPlanCancelDto
{
    public string? Reason { get; set; }
}

/// <summary>Edita un plan mientras está en PLANNED (mismas reglas que la creación:
/// StartDate futura, y si se especifica StartTime/EndTime, StartDate debe ser igual a EndDate).</summary>
public class MassVacationPlanUpdateDto
{
    public int? DepartmentId { get; set; }
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int VacationYear { get; set; }
}

public class MassVacationPlanExclusionSetDto
{
    public int EmployeeId { get; set; }
    public bool IsExcluded { get; set; }
    public string? Reason { get; set; }
}

public class MassVacationPlanExecuteRowResultDto
{
    public int EmployeeId { get; set; }
    public string IdCard { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MassVacationPlanExecuteResultDto
{
    public int PlanId { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalSuccess { get; set; }
    public int TotalFailed { get; set; }
    public List<MassVacationPlanExecuteRowResultDto> Rows { get; set; } = [];
}

/// <summary>Resultado agregado de una corrida del job diario (puede tocar varios planes a la vez).</summary>
public class MassVacationPlanTransitionRunResultDto
{
    public List<MassVacationPlanExecuteResultDto> StartedPlans { get; set; } = [];
    public List<int> FinishedPlanIds { get; set; } = [];
}
