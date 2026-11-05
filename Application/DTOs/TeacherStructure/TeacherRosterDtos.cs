namespace WsUtaSystem.Application.DTOs.TeacherStructure;

/// <summary>Filtros del listado de docentes titulares (API).</summary>
/// <param name="Search">Texto parcial sobre cédula o nombre completo.</param>
/// <param name="CategoryTypeId">Categoría académica (ref_Types ACADEMIC_CATEGORY).</param>
/// <param name="LevelTypeId">Nivel académico (ref_Types ACADEMIC_LEVEL).</param>
/// <param name="DedicationTypeId">Dedicación (ref_Types ACADEMIC_DEDICATION).</param>
/// <param name="DepartmentId">Dependencia; incluye todas sus dependencias hijas (ej. una facultad y sus carreras).</param>
/// <param name="Status">Estado del empleado: ACTIVE (por defecto), INACTIVE o ALL.</param>
/// <param name="PendingScaleAction">true = solo docentes con una acción de escalafón posterior a su estructura.</param>
/// <param name="LadderMismatch">true = solo docentes cuyo escalón del cargo difiere del de su estructura.</param>
public record TeacherRosterFilterDto(
    string? Search = null,
    int? CategoryTypeId = null,
    int? LevelTypeId = null,
    int? DedicationTypeId = null,
    int? DepartmentId = null,
    string? Status = "ACTIVE",
    bool? PendingScaleAction = null,
    bool? LadderMismatch = null,
    int Page = 1,
    int PageSize = 20
);

/// <summary>Una fila del listado de docentes titulares.</summary>
public record TeacherRosterItemDto(
    int TeacherStructureId,
    int EmployeeId,
    string FullName,
    string? IdCard,
    bool EmployeeIsActive,
    int? DepartmentId,
    string? DepartmentName,
    string? FacultyName,
    int? LadderId,
    string? LadderName,
    int? CategoryTypeId,
    string? CategoryName,
    int? LevelTypeId,
    string? LevelName,
    int DedicationTypeId,
    string? DedicationName,
    decimal? WeeklyClassHours,
    DateOnly StructureStartDate,
    DateOnly? AppointmentDate,
    string? AppointmentKind,
    string? AppointmentActionNumber,
    /// <summary>RMU actual del libro de sueldos. Null si el usuario no tiene SALARY_HISTORY.READ o no hay sueldo.</summary>
    decimal? Rmu,
    /// <summary>Documento que origina el RMU (ej. "Promoción en Categoría y Nivel DTH-UTA-PRO-1-2024"). Solo con permiso de sueldos.</summary>
    string? RmuSource,
    /// <summary>true = el documento rige hoy; false = es el último registrado (ya concluido). Solo con permiso de sueldos.</summary>
    bool? RmuInForce,
    /// <summary>true = sueldo de respaldo o ambiguo, conviene revisarlo. Solo con permiso de sueldos.</summary>
    bool? RmuNeedsReview,
    /// <summary>Número de la última acción de escalafón (promoción, recategorización, ubicación o cambio de dedicación) posterior a la estructura.</summary>
    string? PendingScaleActionNumber,
    string? PendingScaleActionType,
    DateOnly? PendingScaleActionDate,
    /// <summary>true = el escalón asignado al cargo del empleado difiere (categoría o nivel) del de su estructura docente.</summary>
    bool LadderMismatch
);

/// <summary>Resumen de conteos para las tarjetas de la página.</summary>
public record TeacherRosterSummaryDto(
    int Total,
    int ActiveCount,
    int InactiveCount,
    IReadOnlyList<TeacherRosterCountDto> ByCategory,
    IReadOnlyList<TeacherRosterCountDto> ByCategoryLevel,
    IReadOnlyList<TeacherRosterCountDto> ByDedication,
    IReadOnlyList<TeacherRosterMatrixCellDto> CategoryByDedication,
    int PendingScaleActionCount,
    int LadderMismatchCount,
    /// <summary>Empleados activos con una acción de escalafón firmada y sin estructura docente activa (no depende de los filtros).</summary>
    int ActiveWithScaleActionWithoutStructure
);

/// <summary>Conteo por un tipo de catálogo (categoría, nivel o dedicación).</summary>
public record TeacherRosterCountDto(int? TypeId, string? Name, int? ParentTypeId, int Total);

/// <summary>Celda categoría × dedicación.</summary>
public record TeacherRosterMatrixCellDto(int? CategoryTypeId, string? CategoryName, int DedicationTypeId, string? DedicationName, int Total);
