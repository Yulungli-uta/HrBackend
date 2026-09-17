namespace WsUtaSystem.Application.DTOs.EmployeeSelfService;

/// <summary>
/// Fila cruda de un permiso reciente, tal como la devuelve
/// <c>HR.sp_GetEmployeeSelfServiceSummary</c>. Se mapea a
/// <see cref="EmployeeSelfServicePermissionDto"/> en el servicio.
/// </summary>
public sealed record EmployeeSelfServicePermissionRow(
    int PermissionId,
    int PermissionTypeId,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    decimal? HourTaken,
    string? Justification
);

/// <summary>
/// Fila cruda de una vacación reciente. <see cref="StartDate"/>/<see cref="EndDate"/> llegan
/// como <see cref="DateTime"/> (tipo SQL <c>date</c> vía Dapper) y se convierten a
/// <see cref="DateOnly"/> en el servicio — el proyecto no registra un TypeHandler de Dapper
/// para DateOnly, así que se evita depender de soporte automático.
/// </summary>
public sealed record EmployeeSelfServiceVacationRow(
    int VacationId,
    DateTime StartDate,
    DateTime EndDate,
    int DaysGranted,
    int DaysTaken,
    string Status
);

/// <summary>
/// Fila cruda de una solicitud interna reciente. No incluye EmployeeFullName/IdCard/
/// DepartmentName — el resumen consulta un solo empleado (el autenticado), así que esos
/// campos se completan en el servicio desde el <see cref="EmployeeSelfServiceProfileDto"/>
/// ya resuelto, evitando repetir los JOINs a Employees/People/Departments que sí hacen
/// falta en el listado paginado general (<c>EmployeeInternalRequestRepository.GetPagedAsync</c>).
/// </summary>
public sealed record EmployeeSelfServiceInternalRequestRow(
    int RequestId,
    string RequestType,
    string Subject,
    string Status,
    DateTime? CreatedAt
);

/// <summary>Última marcación del empleado, o null si nunca ha marcado.</summary>
public sealed record EmployeeSelfServiceLastPunchRow(
    DateTime PunchTime,
    string PunchType
);

/// <summary>
/// Resultado agregado de <c>HR.sp_GetEmployeeSelfServiceSummary</c>: todo lo que
/// <c>EmployeeSelfServiceService.GetSummaryAsync</c> necesita salvo el perfil
/// (que ya se resuelve aparte vía el caché de <c>ICurrentUserService</c>), en una sola ida y
/// vuelta a la base de datos en vez de ~8 secuenciales.
/// </summary>
public sealed record EmployeeSelfServiceSummaryRawData(
    int? VacationAvailableMin,
    int PendingPermissionsCount,
    IReadOnlyList<EmployeeSelfServicePermissionRow> RecentPermissions,
    IReadOnlyList<EmployeeSelfServiceVacationRow> RecentVacations,
    IReadOnlyList<EmployeeCertificate.EmployeeCertificateSummaryDto> RecentCertificates,
    IReadOnlyList<EmployeeSelfServiceInternalRequestRow> RecentInternalRequests,
    EmployeeSelfServiceLastPunchRow? LastPunch,
    int PendingJustificationsCount
);
