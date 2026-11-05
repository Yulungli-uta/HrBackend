namespace WsUtaSystem.Application.DTOs.TeacherStructure;

/// <summary>Filtros normalizados que recibe el repositorio del listado de titulares.</summary>
public sealed record TeacherRosterQuery(
    string? Search,
    int? CategoryTypeId,
    int? LevelTypeId,
    int? DedicationTypeId,
    int? DepartmentId,
    bool? EmployeeIsActive,
    bool? PendingScaleAction,
    bool? LadderMismatch
);

/// <summary>Filtros que NO se aplican al contar un grupo de tarjetas (el grupo no se filtra por sí mismo).</summary>
[Flags]
public enum TeacherRosterIgnore
{
    None = 0,
    /// <summary>Tarjetas de categoría y sus niveles.</summary>
    CategoryAndLevel = 1,
    /// <summary>Tarjetas de dedicación.</summary>
    Dedication = 2,
}

/// <summary>
/// Fila cruda de la consulta del listado. Los nombres de propiedad coinciden con los alias de columna
/// del SQL en <c>TeacherRosterRepository</c>; los indicadores vienen como int (0/1).
/// </summary>
public sealed class TeacherRosterRow
{
    public int TeacherStructureId { get; set; }
    public int EmployeeId { get; set; }
    public string? FullName { get; set; }
    public string? IdCard { get; set; }
    public bool EmployeeIsActive { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? FacultyName { get; set; }
    public int? LadderId { get; set; }
    public string? LadderName { get; set; }
    public int? CategoryTypeId { get; set; }
    public string? CategoryName { get; set; }
    public int? LevelTypeId { get; set; }
    public string? LevelName { get; set; }
    public int DedicationTypeId { get; set; }
    public string? DedicationName { get; set; }
    public decimal? WeeklyClassHours { get; set; }
    public DateTime StructureStartDate { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public string? AppointmentKind { get; set; }
    public string? AppointmentActionNumber { get; set; }
    public decimal? Rmu { get; set; }
    public string? RmuSource { get; set; }
    public bool? RmuInForce { get; set; }
    public int? RmuNeedsReview { get; set; }
    public string? PendingScaleActionNumber { get; set; }
    public string? PendingScaleActionType { get; set; }
    public DateTime? PendingScaleActionDate { get; set; }
    public int PendingScaleFlag { get; set; }
    public int LadderMismatchFlag { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>Grupo agregado (categoría, nivel, dedicación, estado) para las tarjetas.</summary>
public sealed class TeacherRosterGroupRow
{
    public int? CategoryTypeId { get; set; }
    public string? CategoryName { get; set; }
    public int? LevelTypeId { get; set; }
    public string? LevelName { get; set; }
    public int DedicationTypeId { get; set; }
    public string? DedicationName { get; set; }
    public bool EmployeeIsActive { get; set; }
    public int Total { get; set; }
    public int PendingScale { get; set; }
    public int Mismatch { get; set; }
}
