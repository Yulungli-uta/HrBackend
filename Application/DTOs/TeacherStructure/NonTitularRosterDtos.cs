namespace WsUtaSystem.Application.DTOs.TeacherStructure;

/// <summary>Filtros del listado de docentes NO titulares (docentes ocasionales).</summary>
/// <param name="Search">Texto parcial sobre cédula o nombre completo.</param>
/// <param name="DedicationTypeId">Dedicación (ref_Types ACADEMIC_DEDICATION), derivada de la modalidad del contrato.</param>
/// <param name="ContractTypeId">Tipo de contrato docente ocasional (HR.tbl_contract_type).</param>
/// <param name="DepartmentId">Dependencia; incluye todas sus dependencias hijas.</param>
/// <param name="Status">Estado del contrato: ACTIVE = vigente (por defecto), EXPIRED = vencido (empleado activo) o ALL.</param>
public record NonTitularRosterFilterDto(
    string? Search = null,
    int? DedicationTypeId = null,
    int? ContractTypeId = null,
    int? DepartmentId = null,
    string? Status = "ACTIVE",
    int Page = 1,
    int PageSize = 20
);

/// <summary>Una fila del listado de docentes no titulares: el contrato ocasional más reciente de la persona.</summary>
public record NonTitularRosterItemDto(
    int EmployeeId,
    string FullName,
    string? IdCard,
    bool EmployeeIsActive,
    int? DepartmentId,
    string? DepartmentName,
    string? FacultyName,
    string? JobName,
    int? DedicationTypeId,
    string? DedicationName,
    decimal? ContractedHours,
    int? ContractTypeId,
    string? ContractTypeName,
    string? ContractCode,
    DateOnly ContractStartDate,
    DateOnly? ContractEndDate,
    /// <summary>VIGENTE | VENCIDO (según las fechas del contrato frente al día de hoy en Ecuador).</summary>
    string ContractState,
    /// <summary>RMU actual del libro de sueldos. Null si el usuario no tiene SALARY_HISTORY.READ o no hay sueldo.</summary>
    decimal? Rmu,
    string? RmuSource,
    bool? RmuInForce,
    bool? RmuNeedsReview
);

/// <summary>Conteos para las tarjetas de la pestaña "No titulares".</summary>
public record NonTitularRosterSummaryDto(
    int Total,
    int VigenteCount,
    int VencidoCount,
    IReadOnlyList<TeacherRosterCountDto> ByDedication,
    IReadOnlyList<TeacherRosterCountDto> ByContractType
);

/// <summary>Filtros normalizados que recibe el repositorio.</summary>
/// <param name="ContractState">VIGENTE | VENCIDO | null (ambos).</param>
public sealed record NonTitularRosterQuery(
    string? Search,
    int? DedicationTypeId,
    int? ContractTypeId,
    int? DepartmentId,
    string? ContractState
);

/// <summary>Filtros que NO se aplican al contar un grupo de tarjetas (el grupo no se filtra por sí mismo).</summary>
[Flags]
public enum NonTitularRosterIgnore
{
    None = 0,
    Dedication = 1,
    ContractType = 2,
}

/// <summary>Fila cruda de la consulta; los nombres coinciden con los alias del SQL.</summary>
public sealed class NonTitularRosterRow
{
    public int EmployeeId { get; set; }
    public string? FullName { get; set; }
    public string? IdCard { get; set; }
    public bool EmployeeIsActive { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? FacultyName { get; set; }
    public string? JobName { get; set; }
    public int? DedicationTypeId { get; set; }
    public string? DedicationName { get; set; }
    public decimal? ContractedHours { get; set; }
    public int? ContractTypeId { get; set; }
    public string? ContractTypeName { get; set; }
    public string? ContractCode { get; set; }
    public DateTime ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string ContractState { get; set; } = string.Empty;
    public decimal? Rmu { get; set; }
    public string? RmuSource { get; set; }
    public bool? RmuInForce { get; set; }
    public int? RmuNeedsReview { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>Grupo agregado (dedicación, tipo de contrato, estado) para las tarjetas.</summary>
public sealed class NonTitularRosterGroupRow
{
    public int? DedicationTypeId { get; set; }
    public string? DedicationName { get; set; }
    public int? ContractTypeId { get; set; }
    public string? ContractTypeName { get; set; }
    public string ContractState { get; set; } = string.Empty;
    public int Total { get; set; }
}
