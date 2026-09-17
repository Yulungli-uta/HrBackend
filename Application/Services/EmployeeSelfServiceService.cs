using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;
using WsUtaSystem.Application.DTOs.EmployeeSelfService;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Implementación del agregador de autoservicio. Todo lo que expone ya existía en otros
/// módulos (Permisos, Vacaciones, TimeBalances, Certificados, Solicitudes internas) — este
/// servicio solo resuelve el EmployeeId una sola vez y compone las respuestas para el
/// dashboard, evitando que el frontend tenga que hacer 5 llamadas separadas.
/// </summary>
public sealed class EmployeeSelfServiceService : IEmployeeSelfServiceService
{
    private const int MinutesPerWorkday = 480;
    private const int RecentItemsCount = 5;

    private readonly IPermissionsService _permissionsService;
    private readonly IVacationsService _vacationsService;
    private readonly ITimeBalancesService _timeBalancesService;
    private readonly IEmployeeCertificateService _certificateService;
    private readonly IEmployeeInternalRequestService _internalRequestService;
    private readonly IAttendancePunchesService _attendancePunchesService;
    private readonly IJustificationsService _justificationsService;
    private readonly IEmployeeSelfServiceRepository _selfServiceRepository;
    private readonly WsUtaSystem.Application.Common.Interfaces.ICurrentUserService _currentUser;

    public EmployeeSelfServiceService(
        IPermissionsService permissionsService,
        IVacationsService vacationsService,
        ITimeBalancesService timeBalancesService,
        IEmployeeCertificateService certificateService,
        IEmployeeInternalRequestService internalRequestService,
        IAttendancePunchesService attendancePunchesService,
        IJustificationsService justificationsService,
        IEmployeeSelfServiceRepository selfServiceRepository,
        WsUtaSystem.Application.Common.Interfaces.ICurrentUserService currentUser)
    {
        _permissionsService = permissionsService ?? throw new ArgumentNullException(nameof(permissionsService));
        _vacationsService = vacationsService ?? throw new ArgumentNullException(nameof(vacationsService));
        _timeBalancesService = timeBalancesService ?? throw new ArgumentNullException(nameof(timeBalancesService));
        _certificateService = certificateService ?? throw new ArgumentNullException(nameof(certificateService));
        _internalRequestService = internalRequestService ?? throw new ArgumentNullException(nameof(internalRequestService));
        _attendancePunchesService = attendancePunchesService ?? throw new ArgumentNullException(nameof(attendancePunchesService));
        _justificationsService = justificationsService ?? throw new ArgumentNullException(nameof(justificationsService));
        _selfServiceRepository = selfServiceRepository ?? throw new ArgumentNullException(nameof(selfServiceRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <inheritdoc/>
    public async Task<EmployeeSelfServiceProfileDto> GetProfileAsync(int employeeId, CancellationToken ct = default)
    {
        // Reutiliza ICurrentUserService.LoadMeAsync (ya usado en todo el sistema para
        // resolver al empleado autenticado) en vez de escribir una consulta nueva.
        var me = await _currentUser.LoadMeAsync(ct)
            ?? throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

        return new EmployeeSelfServiceProfileDto(
            me.EmployeeID, me.FullName, me.IDCard, me.Email, me.PersonnelEmail,
            me.JobName, me.DepartmentID, me.Department, me.ContractType, me.Schedule,
            me.HireDate, me.ImmediateBossID);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// [2026-09-17] Antes hacia ~9 idas y vueltas secuenciales a SQL Server (una por cada
    /// servicio inyectado abajo). Ahora resuelve todo -salvo el perfil, ya cacheado por
    /// ICurrentUserService- en una sola llamada a <see cref="_selfServiceRepository"/>
    /// (HR.sp_GetEmployeeSelfServiceSummary, ver Database/hr/11_employee_self_service.sql).
    /// GetHistoryAsync sigue usando los servicios de abajo tal cual -- ese endpoint sí
    /// necesita el historial completo, no un resumen acotado.
    /// </remarks>
    public async Task<EmployeeSelfServiceSummaryDto> GetSummaryAsync(int employeeId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(employeeId, ct);
        var raw = await _selfServiceRepository.GetSummaryDataAsync(employeeId, ct);

        var vacationDays = raw.VacationAvailableMin is null
            ? 0m
            : Math.Round(raw.VacationAvailableMin.Value / (decimal)MinutesPerWorkday, 1);

        var recentPermissions = raw.RecentPermissions
            .Select(p => new EmployeeSelfServicePermissionDto(
                p.PermissionId, p.PermissionTypeId, p.StartDate, p.EndDate, p.Status, p.HourTaken, p.Justification))
            .ToList();

        var recentVacations = raw.RecentVacations
            .Select(v => new EmployeeSelfServiceVacationDto(
                v.VacationId, DateOnly.FromDateTime(v.StartDate), DateOnly.FromDateTime(v.EndDate),
                v.DaysGranted, v.DaysTaken, v.Status))
            .ToList();

        // Mismo criterio que el código anterior: cuenta pendientes SOLO entre las 5 más
        // recientes (no el total real) -- ver nota de paridad en el SP.
        var pendingInternalRequests = raw.RecentInternalRequests.Count(r =>
            r.Status is "PENDIENTE" or "EN_REVISION" or "DEVUELTO");
        var recentInternalRequests = raw.RecentInternalRequests
            .Select(r => new EmployeeInternalRequestSummaryDto(
                r.RequestId, employeeId, profile.FullName, profile.IdCard, profile.DepartmentName,
                r.RequestType, r.Subject, r.Status, r.CreatedAt))
            .ToList();

        return new EmployeeSelfServiceSummaryDto(
            profile, vacationDays, raw.PendingPermissionsCount, pendingInternalRequests,
            recentPermissions, recentVacations, raw.RecentCertificates, recentInternalRequests,
            raw.LastPunch?.PunchTime, raw.LastPunch?.PunchType, raw.PendingJustificationsCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmployeeSelfServiceHistoryEntryDto>> GetHistoryAsync(int employeeId, CancellationToken ct = default)
    {
        var entries = new List<EmployeeSelfServiceHistoryEntryDto>();

        var permissions = await _permissionsService.GetByEmployeeId(employeeId, ct);
        entries.AddRange(permissions.Select(p => new EmployeeSelfServiceHistoryEntryDto(
            "PERMISSION", p.PermissionId, "Permiso", p.Status, p.CreatedAt ?? p.StartDate, p.Justification)));

        var vacations = await _vacationsService.GetByEmployeeId(employeeId, ct);
        entries.AddRange(vacations.Select(v => new EmployeeSelfServiceHistoryEntryDto(
            "VACATION", v.VacationId, "Vacaciones", v.Status, v.CreatedAt ?? v.StartDate.ToDateTime(TimeOnly.MinValue), null)));

        var certificates = await _certificateService.GetMyRequestsAsync(
            employeeId, new EmployeeCertificateQueryFilter(null, null, 1, 100), ct);
        entries.AddRange(certificates.Items.Select(c => new EmployeeSelfServiceHistoryEntryDto(
            "CERTIFICATE", c.RequestId, $"Certificado {c.CertificateType}", c.Status, c.CreatedAt ?? DateTime.MinValue, c.Purpose)));

        var internalRequests = await _internalRequestService.GetMyRequestsAsync(
            employeeId, new EmployeeInternalRequestQueryFilter(null, null, null, 1, 100), ct);
        entries.AddRange(internalRequests.Items.Select(r => new EmployeeSelfServiceHistoryEntryDto(
            "INTERNAL_REQUEST", r.RequestId, r.Subject, r.Status, r.CreatedAt ?? DateTime.MinValue, r.RequestType)));

        var justifications = await _justificationsService.GetByEmployeeId(employeeId, ct);
        entries.AddRange(justifications.Select(j => new EmployeeSelfServiceHistoryEntryDto(
            "JUSTIFICATION", j.PunchJustId, "Justificación", j.Status, j.CreatedAt ?? j.JustificationDate ?? DateTime.MinValue, j.Reason)));

        return entries.OrderByDescending(e => e.Date).ToList();
    }
}
