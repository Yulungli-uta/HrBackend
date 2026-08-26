namespace WsUtaSystem.Application.DTOs.PersonnelActions;

// ── Respuestas ───────────────────────────────────────────────────────────────────

/// <summary>DTO de resumen de acción de personal para listados.</summary>
public sealed record PersonnelActionSummaryDto(
    int ActionId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    int ActionTypeId,
    string ActionTypeName,
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,
    string Status,
    int? GeneratedDocumentId,
    DateTime? CreatedAt
);

/// <summary>DTO completo de acción de personal con todos los datos del formulario.</summary>
public sealed record PersonnelActionDetailDto(
    int ActionId,
    int EmployeeId,
    string EmployeeFullName,
    string EmployeeIdCard,
    string EmployeeDepartment,
    string EmployeeJobTitle,
    int ActionTypeId,
    string ActionTypeName,
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,

    // Cargo origen
    int? OriginDepartmentId,
    string? OriginDepartmentName,
    int? OriginJobId,
    string? OriginJobTitle,
    string? OriginBudgetCode,
    /// <summary>Proceso Institucional que quedó registrado como PROPUESTA en la Acción de
    /// Personal previa de este mismo empleado (esa es su clasificación "actual" real, ya
    /// que reachedVigente en su momento). NULL si no hay acción previa — se deja vacío en
    /// el documento, nunca se deriva/inventa de otra fuente (ej. tipo de departamento).
    /// 2026-08-18: reemplaza al intento anterior de derivarlo del tipo de departamento de
    /// origen, que el usuario pidió explícitamente no usar.</summary>
    string? PreviousInstitutionalProcessName,
    /// <summary>Nivel de Gestión de la Acción de Personal previa del empleado — mismo criterio
    /// que PreviousInstitutionalProcessName.</summary>
    string? PreviousManagementLevelName,

    // Cargo destino
    int? DestinationDepartmentId,
    string? DestinationDepartmentName,
    int? DestinationJobId,
    string? DestinationJobTitle,
    string? DestinationBudgetCode,
    /// <summary>Nombre del tipo de departamento de destino — fallback de PROPOSED_INSTITUTIONAL_PROCESS
    /// cuando no se elige manualmente InstitutionalProcess.</summary>
    string? DestinationDepartmentTypeName,
    /// <summary>Descripción del tipo de departamento de destino — fallback de PROPOSED_MANAGEMENT_LEVEL.</summary>
    string? DestinationDepartmentTypeDescription,

    // Datos económicos
    decimal? PreviousRmu,
    decimal? NewRmu,

    // Datos del documento
    string? LegalBasis,
    string? Reason,
    string? Observations,
    string Status,

    // Clasificación de la acción
    bool SwornDeclaration,
    int? InstitutionalProcess,
    string? InstitutionalProcessName,
    int? ManagementLevel,
    string? ManagementLevelName,

    // Régimen laboral del nuevo ingreso (solo cuando EmployeeId es null)
    int? EmployeeTypeId,
    string? EmployeeTypeName,

    // Relaciones
    int? GeneratedDocumentId,
    string? GeneratedDocumentFileName,
    int? ContractId,
    int? MovementId,

    // Responsables del documento (ID + nombre completo + cargo)
    int? DthDirectorId,
    string? DthDirectorName,
    string? DthDirectorTitle,
    int? AuthorityNominatorId,
    string? AuthorityNominatorName,
    string? AuthorityNominatorTitle,
    int? ElaboratorId,
    string? ElaboratorName,
    string? ElaboratorTitle,
    int? ReviewerId,
    string? ReviewerName,
    string? ReviewerTitle,
    int? RegistrarId,
    string? RegistrarName,
    string? RegistrarTitle,

    // Auditoría
    DateTime? CreatedAt,
    int? CreatedBy,
    DateTime? UpdatedAt,
    int? UpdatedBy,

    // 2026-07-06: si el tipo de esta acción participa en la cadena de "vigente"
    // (Nombramiento, Traslado, Encargo, Cambio de Sueldo, Asistencia/Horario).
    bool ActionTypeReachesVigente,

    // 2026-07-06: encontrados sin poblar — el frontend (UploadSignedDocumentDialog.tsx,
    // PersonnelActionActions.tsx) ya los esperaba con este mismo nombre para disparar
    // el aprovisionamiento/deshabilitación automática de AD al cargar el documento
    // firmado, pero el DTO nunca los incluía — siempre llegaban como undefined/false.
    bool ActionTypeRequiresAdUserCreation,
    bool ActionTypeRequiresAdUserDisable,

    // 2026-08-20: Lugar de Trabajo — mismo patrón que InstitutionalProcess/ManagementLevel:
    // WorkplaceName es lo elegido manualmente para esta acción (situación propuesta);
    // PreviousWorkplaceName viene de la acción anterior del empleado (situación actual).
    int? Workplace,
    string? WorkplaceName,
    string? PreviousWorkplaceName,

    // 2026-08-24: clasificación funcional del tipo de acción (ENTRY, MOVEMENT,
    // SALARY_CHANGE, LEAVE, EXIT, DISCIPLINARY, VACATION, VULNERABILITY, SCHEDULE, OTHER).
    // Reemplaza a ActionTypeReachesVigente como señal de "esta acción cambia de puesto/
    // sueldo" en el documento — ReachesVigente es un concepto de nómina/asistencia, no de
    // esto (ver descripción de la columna en HR.tbl_Personnel_Action_Type).
    string? ActionCategory,

    // 2026-08-24: Grupo Ocupacional y Grado del cargo origen/destino (Job.GroupId ->
    // OccupationalGroup -> Degree). Placeholders CURRENT/PROPOSED_OCCUPATIONAL_GROUP y
    // CURRENT/PROPOSED_GRADE existían en la plantilla pero nunca se resolvían.
    string? OriginOccupationalGroupName,
    string? OriginGradeName,
    string? DestinationOccupationalGroupName,
    string? DestinationGradeName
);

// ── Solicitudes ──────────────────────────────────────────────────────────────────

/// <summary>Solicitud para crear una nueva acción de personal.</summary>
public sealed record CreatePersonnelActionRequest(
    int personId,
    int? EmployeeId,
    int ActionTypeId,
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,

    // Cargo origen
    int? OriginDepartmentId,
    int? OriginJobId,
    string? OriginBudgetCode,

    // Cargo destino
    int? DestinationDepartmentId,
    int? DestinationJobId,
    string? DestinationBudgetCode,

    // Datos económicos
    decimal? PreviousRmu,
    decimal? NewRmu,

    // Datos del documento
    string? LegalBasis,
    string? Reason,
    string? Observations,

    // Relaciones
    int? ContractId,
    int? MovementId,

    // Régimen laboral del nuevo ingreso (solo cuando la persona no tiene empleado activo)
    int? EmployeeTypeId,

    // Clasificación de la acción
    bool SwornDeclaration,
    int? InstitutionalProcess,
    int? ManagementLevel,
    int? Workplace,

    // Responsables del documento
    int? DthDirectorId,
    int? AuthorityNominatorId,
    int? ElaboratorId,
    int? ReviewerId,
    int? RegistrarId,

    // Generación automática del documento PDF
    bool GenerateDocument = false,
    Dictionary<string, string>? DocumentOverrides = null,

    /// <summary>true solo desde "Ingresar Histórico": exige que ActionDate/EffectiveDate/EndDate
    /// (si no es el centinela de indefinido) sean anteriores a hoy.</summary>
    bool IsHistoricalEntry = false
);

/// <summary>Solicitud para actualizar una acción de personal existente.</summary>
public sealed record UpdatePersonnelActionRequest(
    string? ActionNumber,
    DateOnly ActionDate,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,
    int? OriginDepartmentId,
    int? OriginJobId,
    string? OriginBudgetCode,
    int? DestinationDepartmentId,
    int? DestinationJobId,
    string? DestinationBudgetCode,
    decimal? PreviousRmu,
    decimal? NewRmu,
    string? LegalBasis,
    string? Reason,
    string? Observations,

    // Régimen laboral del nuevo ingreso
    int? EmployeeTypeId,

    // Clasificación de la acción
    bool SwornDeclaration,
    int? InstitutionalProcess,
    int? ManagementLevel,
    int? Workplace,

    // Responsables del documento
    int? DthDirectorId,
    int? AuthorityNominatorId,
    int? ElaboratorId,
    int? ReviewerId,
    int? RegistrarId
);

/// <summary>
/// Solicitud para corregir una acción de personal ya existente, en cualquier estado.
/// A diferencia de <see cref="UpdatePersonnelActionRequest"/> (solo BORRADOR/GENERADO),
/// exige un motivo obligatorio y queda registrada en HR.Audit (Action=CORRECTION).
/// </summary>
public sealed record CorrectPersonnelActionRequest(
    string Reason,
    UpdatePersonnelActionRequest Data
);

/// <summary>Corrige directamente el Status de una acción, sin pasar por el flujo normal de transición.</summary>
public sealed record CorrectPersonnelActionStatusRequest(
    string Reason,
    string NewStatus
);

/// <summary>Solicitud para aprobar y ejecutar una acción de personal.</summary>
public sealed record ApprovePersonnelActionRequest(
    string? Notes,
    bool GenerateDocumentIfMissing = true
);

/// <summary>Filtros para consultar acciones de personal.</summary>
public sealed record PersonnelActionQueryFilter(
    int? EmployeeId,
    int? ActionTypeId,
    string? Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    /// <summary>Búsqueda libre: cédula del empleado, nombre completo o N° de acción (contiene, sin distinguir mayúsculas).</summary>
    string? Search = null,
    /// <summary>
    /// Filtro opcional elegido por el usuario (distinto de <see cref="AllowedDepartmentIds"/>,
    /// que es la restricción de permisos). Se valida contra DestinationDepartmentId; si la
    /// acción no tiene destino, se usa OriginDepartmentId como referencia.
    /// </summary>
    int? DepartmentId = null,
    int Page = 1,
    int PageSize = 20,
    /// <summary>
    /// Departamentos permitidos según el scope de acceso del usuario (UserAccessScope).
    /// Se valida contra DestinationDepartmentId; si la acción no tiene destino, se usa
    /// OriginDepartmentId como referencia. Null = sin restricción.
    /// </summary>
    IReadOnlyList<int>? AllowedDepartmentIds = null
);

/// <summary>Resultado paginado de acciones de personal.</summary>
public sealed record PagedPersonnelActionResult(
    IReadOnlyList<PersonnelActionSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>Respuesta de creación de acción de personal con documento generado opcional.</summary>
public sealed record CreatePersonnelActionResponse(
    int ActionId,
    string? ActionNumber,
    string Status,
    int? GeneratedDocumentId,
    string? PdfBase64,
    string? FileName
);

/// <summary>Solicitud para subir el documento firmado físicamente.</summary>
public sealed record UploadSignedDocumentRequest(
    /// <summary>ID del archivo físico previamente almacenado en tbl_StoredFiles.</summary>
    int StoredFileId,
    string? Comment,
    /// <summary>
    /// True cuando el documento se carga desde la pantalla de "Ingresar Histórico" —
    /// un registro que ya concluyó, no un evento en curso. En ese caso el servicio
    /// omite el aprovisionamiento/bloqueo de cuenta AD y el cierre de régimen por
    /// separación (no aplican a un backfill de papel), aunque el registro y el
    /// documento se guardan igual. Default false: no cambia el comportamiento normal.
    /// </summary>
    bool IsHistoricalEntry = false
);

/// <summary>Solicitud para anular una acción de personal.</summary>
public sealed record CancelPersonnelActionRequest(
    string Reason
);

/// <summary>Solicitud de previsualización del documento sin guardar en BD.</summary>
public sealed record PreviewPersonnelActionRequest(
    int EmployeeId,
    Dictionary<string, string>? Overrides
);

/// <summary>DTO de una entrada del historial de estados.</summary>
public sealed record PersonnelActionStatusHistoryDto(
    int HistoryId,
    int ActionId,
    int? StatusTypeId,
    string? FromStatus,
    string StatusCode,
    string? Comment,
    int? ChangedBy,
    DateTime ChangedAt
);
