namespace WsUtaSystem.Application.Infrastructure.Academic;

/// <summary>
/// Registro de matrícula proveniente del origen externo (tabla SQL o API).
/// Es un DTO de lectura: no se persiste directamente, se usa para sincronizar con HR.
/// </summary>
public record StudentEnrollmentRecord(
    /// <summary>Cédula de identidad. Se almacena en HR.tbl_People.IdCard.</summary>
    string IdCard,

    string FirstName,
    string LastName,
    string PeriodCode,

    string? Email = null,
    string? Program = null,
    string? Faculty = null,

    /// <summary>Código del estudiante en el sistema académico externo.</summary>
    string? ExternalStudentCode = null
);
