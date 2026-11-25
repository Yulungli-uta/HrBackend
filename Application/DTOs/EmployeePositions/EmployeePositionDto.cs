namespace WsUtaSystem.Application.DTOs.EmployeePositions;

/// <summary>Posición vigente de un empleado (principal, segundo régimen, autoridad o temporal).</summary>
public record EmployeePositionDto(
    int EmployeeId,
    int PersonId,
    string? IdCard,
    string? FullName,
    string PositionType,
    int? LaborRegimeId,
    string? LaborRegimeName,
    int? JobId,
    string? JobName,
    string? JobBaseName,
    string? JobSource,
    int? DepartmentId,
    string? DepartmentName,
    string? DedicationCode,
    string? DedicationName,
    string? DedicationSource,
    string? ModalityName,
    decimal? ContractedHours,
    string? DocumentNumber,
    DateTime? StartDate,
    DateTime? EndDate);
