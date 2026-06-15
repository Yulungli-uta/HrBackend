namespace WsUtaSystem.Application.DTOs.TeacherStructure;

/// <summary>Datos completos de una estructura docente.</summary>
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
