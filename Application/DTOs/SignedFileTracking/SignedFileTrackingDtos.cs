namespace WsUtaSystem.Application.DTOs.SignedFileTracking;

/// <summary>Filtros de las pantallas "Acciones/Contratos con archivo firmado". Siempre paginado.</summary>
/// <param name="Search">Búsqueda libre: cédula, nombre de la persona o número de documento (contiene).</param>
/// <param name="TypeId">Tipo de acción (ActionTypeId) o de contrato (ContractTypeId).</param>
/// <param name="Status">Solo acciones: texto del estado (VIGENTE, FINALIZADO, ...).</param>
/// <param name="StatusTypeId">Solo contratos: TypeId del estado en ref_Types (CONTRACT_STATUS).</param>
/// <param name="DepartmentId">Departamento elegido por el usuario.</param>
/// <param name="StartDate">Desde (fecha efectiva de la acción / inicio del contrato).</param>
/// <param name="EndDate">Hasta (misma fecha de referencia).</param>
/// <param name="AllowedDepartmentIds">Restricción de permisos por departamento (null = sin restricción). La asigna el servicio, no el cliente.</param>
public sealed record SignedFileTrackingFilter(
    string? Search = null,
    int? TypeId = null,
    string? Status = null,
    int? StatusTypeId = null,
    int? DepartmentId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int Page = 1,
    int PageSize = 20,
    IReadOnlyList<int>? AllowedDepartmentIds = null);

/// <summary>Acción de personal con archivo firmado registrado.</summary>
public sealed record PersonnelActionSignedFileDto(
    int ActionId,
    string? ActionNumber,
    string? PersonIdCard,
    string? PersonName,
    string? DepartmentName,
    string? ActionTypeName,
    string? Status,
    DateOnly? EffectiveDate,
    DateOnly? EndDate,
    string? ElaboratorIdCard,
    string? ElaboratorName,
    string? SignedFileName,
    long SignedFileSizeBytes,
    DateTime? SignedFileUploadedAt);

/// <summary>Contrato con archivo firmado registrado.</summary>
public sealed record ContractSignedFileDto(
    int ContractId,
    string? ContractCode,
    string? PersonIdCard,
    string? PersonName,
    string? DepartmentName,
    string? ContractTypeName,
    string? StatusName,
    DateTime StartDate,
    DateTime EndDate,
    string? CreatedByIdCard,
    string? CreatedByName,
    string? SignedFileName,
    long SignedFileSizeBytes,
    DateTime? SignedFileUploadedAt);

/// <summary>Resultado de pedir la vista previa del archivo firmado.</summary>
public enum SignedFilePreviewStatus
{
    /// <summary>El archivo se leyó correctamente.</summary>
    Ok,
    /// <summary>El registro no existe, no tiene archivo activo o el usuario no tiene alcance sobre su departamento.</summary>
    RecordNotFound,
    /// <summary>El registro tiene enlace, pero el archivo no está en el servidor de archivos.</summary>
    FileMissing,
    /// <summary>No se pudo leer el servidor de archivos (red, permisos, timeout).</summary>
    StoreUnavailable
}

/// <summary>Contenido del archivo firmado o el motivo por el que no se pudo entregar.</summary>
public sealed record SignedFilePreviewResult(
    SignedFilePreviewStatus Status,
    byte[]? Content = null,
    string? ContentType = null,
    string? FileName = null);
