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
    /// <summary>Titular = CategoryTypeId IN (2012,2013,2014). No Titular = resto.</summary>
    bool IsTitular
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
