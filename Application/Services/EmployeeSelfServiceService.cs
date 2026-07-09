using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;
using WsUtaSystem.Application.DTOs.EmployeeSelfService;
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
    private readonly WsUtaSystem.Application.Common.Interfaces.ICurrentUserService _currentUser;

    public EmployeeSelfServiceService(
        IPermissionsService permissionsService,
        IVacationsService vacationsService,
        ITimeBalancesService timeBalancesService,
        IEmployeeCertificateService certificateService,
        IEmployeeInternalRequestService internalRequestService,
        IAttendancePunchesService attendancePunchesService,
        IJustificationsService justificationsService,
        WsUtaSystem.Application.Common.Interfaces.ICurrentUserService currentUser)
    {
        _permissionsService = permissionsService ?? throw new ArgumentNullException(nameof(permissionsService));
        _vacationsService = vacationsService ?? throw new ArgumentNullException(nameof(vacationsService));
        _timeBalancesService = timeBalancesService ?? throw new ArgumentNullException(nameof(timeBalancesService));
        _certificateService = certificateService ?? throw new ArgumentNullException(nameof(certificateService));
        _internalRequestService = internalRequestService ?? throw new ArgumentNullException(nameof(internalRequestService));
        _attendancePunchesService = attendancePunchesService ?? throw new ArgumentNullException(nameof(attendancePunchesService));
        _justificationsService = justificationsService ?? throw new ArgumentNullException(nameof(justificationsService));
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
    public async Task<EmployeeSelfServiceSummaryDto> GetSummaryAsync(int employeeId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(employeeId, ct);

        var balance = await _timeBalancesService.GetByIdAsync(employeeId, ct);
        var vacationDays = balance is null ? 0m : Math.Round(balance.VacationAvailableMin / (decimal)MinutesPerWorkday, 1);

        var permissions = (await _permissionsService.GetByEmployeeId(employeeId, ct))
            .OrderByDescending(p => p.CreatedAt ?? p.StartDate)
            .ToList();
        var pendingPermissions = permissions.Count(p => p.Status == "Pending");
        var recentPermissions = permissions.Take(RecentItemsCount)
            .Select(p => new EmployeeSelfServicePermissionDto(
                p.PermissionId, p.PermissionTypeId, p.StartDate, p.EndDate, p.Status, p.HourTaken, p.Justification))
            .ToList();

        var vacations = (await _vacationsService.GetByEmployeeId(employeeId, ct))
            .OrderByDescending(v => v.CreatedAt)
            .Take(RecentItemsCount)
            .Select(v => new EmployeeSelfServiceVacationDto(
                v.VacationId, v.StartDate, v.EndDate,
                v.DaysGranted, v.DaysTaken, v.Status))
            .ToList();

        var certificatesPage = await _certificateService.GetMyRequestsAsync(
            employeeId, new EmployeeCertificateQueryFilter(null, null, 1, RecentItemsCount), ct);

        var internalRequestsPage = await _internalRequestService.GetMyRequestsAsync(
            employeeId, new EmployeeInternalRequestQueryFilter(null, null, null, 1, RecentItemsCount), ct);
        var pendingInternalRequests = internalRequestsPage.Items.Count(r =>
            r.Status is "PENDIENTE" or "EN_REVISION" or "DEVUELTO");

        var lastPunch = (await _attendancePunchesService.GetLastPunchAsync(employeeId, ct))
            .OrderByDescending(p => p.PunchTime)
            .FirstOrDefault();

        var pendingJustifications = (await _justificationsService.GetByEmployeeId(employeeId, ct))
            .Count(j => j.Status == "PENDING");

        return new EmployeeSelfServiceSummaryDto(
            profile, vacationDays, pendingPermissions, pendingInternalRequests,
            recentPermissions, vacations, certificatesPage.Items, internalRequestsPage.Items,
            lastPunch?.PunchTime, lastPunch?.PunchType, pendingJustifications);
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
