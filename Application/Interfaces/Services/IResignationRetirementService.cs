using WsUtaSystem.Application.DTOs.ResignationRetirement;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IResignationRetirementService
{
    /// <summary>Información consolidada del empleado autenticado, para la pantalla de creación.</summary>
    Task<EmployeeConsolidatedInfoDto> GetCurrentEmployeeInfoAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Crea una solicitud para el empleado autenticado. EmployeeId nunca viene del frontend.</summary>
    Task<ResignationRetirementDetailDto> CreateAsync(int employeeId, CreateResignationRetirementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Crea una solicitud en nombre de un empleado que no puede/no logra hacerla él mismo —
    /// exclusivo de Recursos Humanos. A diferencia de <see cref="CreateAsync"/>, permite una
    /// ProposedExitDate ya pasada. El resto del trámite (revisión, aprobación con documento
    /// firmado, cierre de régimen) sigue el mismo flujo sin cambios.
    /// </summary>
    Task<ResignationRetirementDetailDto> CreateOnBehalfAsync(int createdByEmployeeId, CreateResignationRetirementOnBehalfRequest request, CancellationToken ct = default);

    /// <summary>Actualiza una solicitud propia, solo en estado PENDIENTE o DEVUELTO.</summary>
    Task<ResignationRetirementDetailDto> UpdateAsync(int requestId, int employeeId, UpdateResignationRetirementRequest request, CancellationToken ct = default);

    /// <summary>Cancela una solicitud propia (el dueño desiste antes de que RRHH resuelva).</summary>
    Task CancelOwnAsync(int requestId, int employeeId, CancelResignationRetirementRequest request, CancellationToken ct = default);

    /// <summary>Solicitudes del empleado autenticado. Filtra siempre por su propio EmployeeId.</summary>
    Task<PagedResignationRetirementResult> GetMyRequestsAsync(int employeeId, ResignationRetirementQueryFilter filter, CancellationToken ct = default);

    /// <summary>Detalle de una solicitud propia; lanza si no pertenece al empleado autenticado.</summary>
    Task<ResignationRetirementDetailDto> GetMyRequestDetailAsync(int requestId, int employeeId, CancellationToken ct = default);

    /// <summary>Listado para RRHH, ya filtrado por scope de departamento.</summary>
    Task<PagedResignationRetirementResult> GetPagedAsync(ResignationRetirementQueryFilter filter, CancellationToken ct = default);

    /// <summary>Detalle completo para RRHH.</summary>
    Task<ResignationRetirementDetailDto> GetDetailByIdAsync(int requestId, CancellationToken ct = default);

    Task<ResignationRetirementDetailDto> ApproveAsync(int requestId, int reviewedBy, ReviewResignationRetirementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Aprueba la solicitud exigiendo el documento firmado (StoredFileId): crea y finaliza la
    /// acción de personal de desvinculación vinculada (RENUNCIA_JUBILACION), lo que dispara
    /// automáticamente el bloqueo de la cuenta institucional y, si el empleado tenía un
    /// contrato vigente, su cierre a RENUNCIA. Reemplaza a <see cref="ApproveAsync"/> como el
    /// flujo real de aprobación — ese método queda solo por compatibilidad.
    /// </summary>
    Task<ResignationRetirementDetailDto> UploadSignedDocumentAsync(int requestId, int reviewedBy, ApproveResignationRetirementRequest request, CancellationToken ct = default);

    Task<ResignationRetirementDetailDto> RejectAsync(int requestId, int reviewedBy, ReviewResignationRetirementRequest request, CancellationToken ct = default);

    Task<ResignationRetirementDetailDto> ReturnAsync(int requestId, int reviewedBy, ReviewResignationRetirementRequest request, CancellationToken ct = default);

    /// <summary>Cancelación por parte de RRHH (distinta de CancelOwnAsync, requiere permiso administrativo).</summary>
    Task<ResignationRetirementDetailDto> HrCancelAsync(int requestId, int cancelledBy, CancelResignationRetirementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Genera (o regenera) la carta de renuncia/jubilación en PDF, lista para descargar,
    /// imprimir, firmar y volver a subir como documento firmado.
    /// </summary>
    Task<ResignationRetirementDetailDto> GenerateDocumentAsync(int requestId, int employeeId, CancellationToken ct = default);

    Task<(byte[] Bytes, string FileName, string ContentType)> DownloadMyDocumentAsync(int requestId, int employeeId, CancellationToken ct = default);

    Task<(byte[] Bytes, string FileName, string ContentType)> DownloadDocumentAsync(int requestId, CancellationToken ct = default);
}
