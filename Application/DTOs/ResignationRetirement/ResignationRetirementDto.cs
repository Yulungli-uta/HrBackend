namespace WsUtaSystem.Application.DTOs.ResignationRetirement;

// ── Constantes ───────────────────────────────────────────────────────────────────

public static class ResignationRetirementRequestType
{
    public const string Resignation = "RESIGNATION";
    public const string Retirement = "RETIREMENT";
}

public static class ResignationRetirementStatus
{
    public const string Pendiente = "PENDIENTE";
    public const string EnRevision = "EN_REVISION";
    public const string Devuelto = "DEVUELTO";
    public const string Aprobado = "APROBADO";
    public const string Rechazado = "RECHAZADO";
    public const string Anulado = "ANULADO";
}

/// <summary>
/// DirectoryCode/EntityType del documento firmado de renuncia/jubilación, compartidos entre
/// el servicio y el repositorio para no duplicar los literales de string.
/// </summary>
public static class ResignationRetirementDocument
{
    public const string DirectoryCode = "HR_RESIGNATION_RETIREMENT";
    public const string EntityType = "RESIGNATION_RETIREMENT_REQUEST";
}

// ── Información consolidada del empleado ──────────────────────────────────────────

/// <summary>
/// Información consolidada del empleado solicitante, resuelta en backend a partir del
/// usuario autenticado. Se muestra de solo lectura en la pantalla de creación y en el
/// detalle de revisión de Recursos Humanos.
/// </summary>
public sealed record EmployeeConsolidatedInfoDto(
    int EmployeeId,
    int? PersonId,
    string IdCard,
    string FullName,
    string? Email,
    string? PersonalEmail,
    string? Phone,

    string? JobTitle,
    int? DepartmentId,
    string? DepartmentName,
    int? LaborRegimeTypeId,
    string? LaborRegimeName,
    string? ContractTypeName,
    DateOnly HireDate,
    int? ImmediateBossId,
    string? ImmediateBossName,

    // Contrato o acción de personal vigente (fuente única de verdad del cargo/sueldo actual)
    string? VigenteSourceType,   // "CONTRACT" | "PERSONNEL_ACTION" | null
    int? VigenteSourceId,
    string? VigenteDocumentNumber,
    DateOnly? VigenteStartDate,
    DateOnly? VigenteEndDate,
    string? VigenteJobTitle,
    string? VigenteDepartmentName,

    // Información adicional para RRHH
    decimal VacationAvailableDays,
    int ServiceTimeYears,
    int ServiceTimeMonths,

    // Elegibilidad de jubilación (edad o años de servicio, según HR.tbl_Parameters).
    // Solo informativo — no bloquea la creación de la solicitud.
    int? Age,
    bool IsRetirementEligible,
    string? RetirementEligibilityNote
);

// ── Respuestas ───────────────────────────────────────────────────────────────────

public sealed record ResignationRetirementSummaryDto(
    int RequestId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    string? DepartmentName,
    string RequestType,
    DateOnly RequestDate,
    DateOnly ProposedExitDate,
    string Status,
    DateTime? CreatedAt
);

public sealed record ResignationRetirementDetailDto(
    int RequestId,
    string RequestType,
    DateOnly RequestDate,
    DateOnly ProposedExitDate,
    string? Reason,
    string? AdditionalNotes,
    string Status,
    int? LinkedPersonnelActionId,
    int? GeneratedDocumentId,
    string? GeneratedDocumentFileName,

    EmployeeConsolidatedInfoDto Employee,

    DateTime? CreatedAt,
    int? CreatedBy,
    string? CreatedByName,
    DateTime? UpdatedAt,
    int? UpdatedBy,
    DateTime? ApprovedAt,
    int? ApprovedBy,
    string? ApprovedByName,
    DateTime? RejectedAt,
    int? RejectedBy,
    string? RejectedByName,
    DateTime? CancelledAt,
    int? CancelledBy,
    string? CancelledByName,

    IReadOnlyList<ResignationRetirementStatusHistoryDto> History,
    byte[] RowVersion,

    /// <summary>
    /// Documento(s) firmado(s) subidos por el empleado (HR.TBL_StoredFile, DirectoryCode
    /// HR_RESIGNATION_RETIREMENT / EntityType RESIGNATION_RETIREMENT_REQUEST). La UI limita
    /// la carga a un solo archivo; se expone como lista por si históricamente quedó más de uno.
    /// </summary>
    IReadOnlyList<SignedDocumentSummaryDto> SupportingDocuments
);

/// <summary>Resumen de un documento firmado adjunto a la solicitud, para revisión de RRHH.</summary>
public sealed record SignedDocumentSummaryDto(
    int FileId,
    Guid FileGuid,
    string? OriginalFileName,
    DateTime? UploadedAt
);

public sealed record ResignationRetirementStatusHistoryDto(
    int HistoryId,
    int RequestId,
    string? PreviousStatus,
    string NewStatus,
    string Action,
    string? Observation,
    DateTime CreatedAt,
    int CreatedBy
);

public sealed record PagedResignationRetirementResult(
    IReadOnlyList<ResignationRetirementSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// ── Solicitudes ──────────────────────────────────────────────────────────────────

/// <summary>
/// Solicitud de creación. Nunca incluye EmployeeId — se resuelve en backend desde
/// el usuario autenticado (<see cref="Common.Interfaces.ICurrentUserService"/>).
/// </summary>
public sealed record CreateResignationRetirementRequest(
    string RequestType,
    DateOnly ProposedExitDate,
    string? Reason,
    string? AdditionalNotes
);

/// <summary>
/// Actualización permitida solo cuando la solicitud está en PENDIENTE o DEVUELTO
/// y pertenece al usuario autenticado.
/// </summary>
public sealed record UpdateResignationRetirementRequest(
    DateOnly ProposedExitDate,
    string? Reason,
    string? AdditionalNotes,
    byte[] RowVersion
);

/// <summary>Acción de revisión de Recursos Humanos: aprobar, rechazar, devolver o cancelar.</summary>
public sealed record ReviewResignationRetirementRequest(
    string? Observation,
    byte[] RowVersion
);

/// <summary>
/// Aprobación con documento firmado obligatorio: RRHH sube la carta de renuncia/jubilación
/// ya firmada (StoredFileId, obtenido subiendo el archivo antes vía el endpoint genérico de
/// documentos). Dispara la creación de la acción de personal de desvinculación, que a su vez
/// bloquea la cuenta institucional del empleado y, si corresponde, cierra el contrato vigente.
/// </summary>
public sealed record ApproveResignationRetirementRequest(
    int StoredFileId,
    string? Observation,
    byte[] RowVersion
);

public sealed record CancelResignationRetirementRequest(
    string Reason,
    byte[] RowVersion
);

public sealed record ResignationRetirementQueryFilter(
    int? EmployeeId,
    string? RequestType,
    string? Status,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? DepartmentId,
    int Page = 1,
    int PageSize = 20,
    /// <summary>Departamentos permitidos según UserAccessScope. Null = sin restricción.</summary>
    IReadOnlyList<int>? AllowedDepartmentIds = null
);
