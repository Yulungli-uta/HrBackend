namespace WsUtaSystem.Application.DTOs.TeacherStructure;

/// <summary>
/// Datos completos de una estructura docente. <c>Rmu</c> es un campo LEGADO (puede estar vacío o desactualizado): el sueldo
/// vigente se obtiene del libro de sueldos (<c>HR.vw_EmployeeCurrentSalary</c>), p. ej. vía <c>teacher-roster</c>.
/// </summary>
public record TeacherStructureDto(
    int TeacherStructureId,
    int EmployeeId,
    string EmployeeFullName,
    string? EmployeeIdCard,
    int? LadderId,
    string? LadderName,
    int DedicationTypeId,
    string DedicationName,
    decimal? WeeklyClassHours,
    decimal? HourValue,
    decimal? Rmu,
    int? DepartmentId,
    string? DepartmentName,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    bool EligiblePromotion,
    bool EligibleRecategory,
    bool EligibleDedicChg
);

/// <summary>Datos requeridos para crear una estructura docente.</summary>
public record TeacherStructureCreateDto(
    int EmployeeId,
    int? LadderId,
    int DedicationTypeId,
    decimal? WeeklyClassHours,
    decimal? HourValue,
    decimal? Rmu,
    int? DepartmentId,
    DateOnly StartDate,
    DateOnly? EndDate
);

/// <summary>Datos actualizables de una estructura docente.</summary>
public record TeacherStructureUpdateDto(
    int? LadderId,
    int DedicationTypeId,
    decimal? WeeklyClassHours,
    decimal? HourValue,
    decimal? Rmu,
    int? DepartmentId,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool EligiblePromotion,
    bool EligibleRecategory,
    bool EligibleDedicChg
);

/// <summary>
/// Cambio de escalafón y/o dedicación de un docente (promoción, recategorización, cambio de dedicación).
/// No edita la estructura vigente: la cierra el día anterior a <paramref name="EffectiveDate"/> y crea una nueva,
/// de modo que el historial queda intacto.
/// </summary>
/// <param name="NewLadderId">Escalón titular al que pasa el docente.</param>
/// <param name="NewDedicationTypeId">Dedicación (ref_Types ACADEMIC_DEDICATION); debe ser coherente con el escalón.</param>
/// <param name="WeeklyClassHours">Horas semanales; si no se envía se asume 40 (tiempo completo) o 20 (medio tiempo). Obligatoria para tiempo parcial.</param>
/// <param name="EffectiveDate">Fecha desde la que rige la nueva estructura (no puede ser futura ni anterior al inicio de la actual).</param>
/// <param name="DepartmentId">Dependencia; si no se envía se conserva la de la estructura vigente.</param>
/// <param name="Reason">Motivo o documento que respalda el cambio (ej. número de la acción de personal). Obligatorio, queda auditado.</param>
public record TeacherStructureApplyChangeDto(
    int NewLadderId,
    int NewDedicationTypeId,
    decimal? WeeklyClassHours,
    DateOnly EffectiveDate,
    int? DepartmentId,
    string Reason
);

/// <summary>Filtros para listado paginado.</summary>
public record TeacherStructureFilterDto(
    int? EmployeeId,
    int? DedicationTypeId,
    int? LadderId,
    int? DepartmentId,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20
);
