namespace WsUtaSystem.Application.DTOs.AcademicLadder;

/// <summary>Datos completos de un escalafón / cargo docente LOES.</summary>
public record AcademicLadderDto(
    int LadderId,
    string Code,
    string Name,
    int? CategoryTypeId,
    string? CategoryName,
    int? LevelTypeId,
    string? LevelName,
    int? DedicationTypeId,
    string? DedicationName,
    decimal? BaseRmu,
    int Sequence,
    int? NextLadderId,
    string? NextLadderName,
    int? MinYearsService,
    bool IsActive,
    /// <summary>Titular = categoría TITULAR_PRINCIPAL / TITULAR_AGREGADO / TITULAR_AUXILIAR (por nombre). No Titular = resto.</summary>
    bool IsTitular,
    /// <summary>Docentes con estructura docente activa asignada a este escalón.</summary>
    int ActiveTeachers = 0
);

/// <summary>Datos para crear un cargo docente LOES.</summary>
public record AcademicLadderCreateDto(
    string Code,
    string Name,
    int? CategoryTypeId,
    int? LevelTypeId,
    int? DedicationTypeId,
    decimal? BaseRmu,
    int Sequence,
    int? NextLadderId,
    int? MinYearsService
);

/// <summary>Datos actualizables de un cargo docente LOES.</summary>
public record AcademicLadderUpdateDto(
    string Name,
    int? CategoryTypeId,
    int? LevelTypeId,
    int? DedicationTypeId,
    decimal? BaseRmu,
    int Sequence,
    int? NextLadderId,
    int? MinYearsService,
    bool IsActive
);
