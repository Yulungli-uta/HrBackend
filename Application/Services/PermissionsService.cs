using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage; // GetDbTransaction()
using System.Security.Claims;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.DTOs.TimeBalances;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class PermissionsService : Service<Permissions, int>, IPermissionsService
{
    private readonly IPermissionsRepository _repository;
    private readonly IVacationBalanceAdjustmentService _balanceAdjustment;
    private readonly AppDbContext _db;

    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IvwEmployeeDetailsService _employeeDetails;
    private readonly IUserActionPermissionService _actionPermissions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PermissionsService> _logger;

    public PermissionsService(
        IPermissionsRepository repo,
        IVacationBalanceAdjustmentService balanceAdjustment,
        AppDbContext db,
        IEmailBuilder emailBuilder,
        ICurrentUserService currentUser,
        IvwEmployeeDetailsService employeeDetails,
        IUserActionPermissionService actionPermissions,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<PermissionsService> logger
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _balanceAdjustment = balanceAdjustment ?? throw new ArgumentNullException(nameof(balanceAdjustment));
        _db = db ?? throw new ArgumentNullException(nameof(db));

        // IMPORTANTE: tu código original tenía esta línea comentada => _emailBuilder quedaba null y causaba NullReference
        _emailBuilder = emailBuilder ?? throw new ArgumentNullException(nameof(emailBuilder));

        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails = employeeDetails ?? throw new ArgumentNullException(nameof(employeeDetails));
        _actionPermissions = actionPermissions ?? throw new ArgumentNullException(nameof(actionPermissions));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// El endpoint PUT /permissions/{id} mezcla edición general y aprobar/rechazar
    /// (normal o médico) en una sola acción HTTP — un solo [RequirePermission] estático
    /// no alcanza. Resuelve dinámicamente el código requerido según la transición de
    /// estado solicitada y si el tipo de permiso es médico (HR.tbl_PermissionTypes.IsMedical),
    /// y respeta Authorization:ShadowMode igual que el atributo (solo advierte, no bloquea,
    /// hasta que la matriz esté validada).
    /// </summary>
    private async Task EnsureUpdatePermissionAsync(Permissions current, string newStatus, CancellationToken ct)
    {
        string requiredCode;

        if (newStatus is "APPROVED" or "REJECTED")
        {
            var isMedical = await _db.Set<PermissionTypes>()
                .Where(pt => pt.TypeId == current.PermissionTypeId)
                .Select(pt => pt.IsMedical)
                .FirstOrDefaultAsync(ct);

            requiredCode = (isMedical, newStatus) switch
            {
                (true, "APPROVED") => "PERMISSIONS_LICENSES.APPROVE_MEDICAL",
                (true, "REJECTED") => "PERMISSIONS_LICENSES.REJECT_MEDICAL",
                (false, "APPROVED") => "PERMISSIONS_LICENSES.APPROVE",
                _ => "PERMISSIONS_LICENSES.REJECT",
            };
        }
        else
        {
            requiredCode = "PERMISSIONS_LICENSES.UPDATE";
        }

        var roles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];
        var hasPermission = await _actionPermissions.HasPermissionAsync(roles, requiredCode, ct);

        if (!hasPermission)
        {
            var shadowMode = _configuration["Authorization:ShadowMode"] is null
                || (bool.TryParse(_configuration["Authorization:ShadowMode"], out var sm) && sm);

            if (shadowMode)
            {
                _logger.LogWarning(
                    "[RequirePermission:SHADOW] Actualización de permiso {PermissionId} habría sido bloqueada por falta de {Code}",
                    current.PermissionId, requiredCode);
            }
            else
            {
                throw new BusinessRuleException($"No tiene permiso '{requiredCode}' para realizar esta acción.");
            }
        }
    }

    /// <summary>
    /// Cada reserva usa un ID único (no uno fijo por PermissionID) porque un mismo permiso
    /// puede pasar por varios ciclos reserva→libera→reserva (rechazar y volver a pendiente)
    /// — con un ID fijo, la segunda reserva chocaría contra el movimiento de la primera (ya
    /// liberado) y se rechazaría como "reserva duplicada". Hueco cerrado 2026-07-22 (mismo
    /// que en VacationsService).
    /// </summary>
    private static string NewPermissionReserveSourceId(int permissionId)
        => $"PERM_RESERVE|{permissionId}|{Guid.NewGuid():N}";

    /// <summary>Encuentra la reserva actualmente activa (ni liberada ni consumida) de este permiso, si existe.</summary>
    private async Task<string?> FindActivePermissionReserveSourceIdAsync(int permissionId, CancellationToken ct)
    {
        var prefix = $"PERM_RESERVE|{permissionId}|";
        var candidates = await _db.TimeBalanceMovements.AsNoTracking()
            .Where(m => m.SourceModule == "PERMISSION_RESERVE" && m.SourceID != null && m.SourceID.StartsWith(prefix))
            .OrderByDescending(m => m.MovementID)
            .Select(m => m.SourceID!)
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            var released = await _db.TimeBalanceMovements.AsNoTracking().AnyAsync(m => m.SourceID == candidate + "|REL", ct);
            if (released) continue;

            var consumed = await _db.TimeBalanceMovements.AsNoTracking().AnyAsync(m => m.SourceID == candidate + "|USE", ct);
            if (consumed) continue;

            return candidate;
        }

        return null;
    }

    public Task<IEnumerable<Permissions>> GetByEmployeeId(int employeeId, CancellationToken ct)
        => _repository.GetByEmployeeId(employeeId, ct);

    public Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
        => _repository.GetByImmediateBossId(immediateBossId, ct);

    public Task<IEnumerable<Permissions>> GetByImmediateBossIdNonMedical(int employeeId, CancellationToken ct)
    { 
        //=> _repository.GetByImmediateBossIdNonMedical(employeeId, ct);
        //_logger.LogInformation("**************** accede a GetByImmediateBossIdNonMedical");
        return _repository.GetByImmediateBossIdNonMedical(employeeId, ct);
    }

    public Task<IEnumerable<Permissions>> GetPendingMedicalPermissions(CancellationToken ct)
    {
        //=> _repository.GetPendingMedicalPermissions(ct);        
        return _repository.GetPendingMedicalPermissions(ct);
    }

    private static readonly string[] ActivePermissionStatuses = { "PENDING", "APPROVED" };
    private static readonly string[] ActiveVacationStatuses = { "PLANNED", "INPROGRESS", "APPROVED" };

    /// <summary>
    /// Bloquea crear un permiso que se superponga con otro permiso activo, o con una
    /// vacación activa, del mismo empleado. Antes solo se validaba en el frontend.
    /// </summary>
    private async Task EnsureNoOverlapAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var overlapsPermission = await _db.Permissions.AsNoTracking()
            .Where(p => p.EmployeeId == employeeId
                     && ActivePermissionStatuses.Contains(p.Status.ToUpper())
                     && p.StartDate <= endDate && p.EndDate >= startDate)
            .AnyAsync(ct);

        if (overlapsPermission)
            throw new BusinessRuleException("Ya existe un permiso activo que se superpone con estas fechas.");

        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);

        var overlapsVacation = await _db.Vacations.AsNoTracking()
            .Where(v => v.EmployeeId == employeeId
                     && ActiveVacationStatuses.Contains(v.Status.ToUpper())
                     && v.StartDate <= endDateOnly && v.EndDate >= startDateOnly)
            .AnyAsync(ct);

        if (overlapsVacation)
            throw new BusinessRuleException("Ya existe una solicitud de vacaciones activa que se superpone con estas fechas.");
    }

    /// <summary>
    /// Minutos a reservar por un permiso con cargo a vacaciones — mismo cálculo que
    /// HR.sp_hr_ReservePermissionBalance (HourTaken si viene, si no días laborables x
    /// jornada; factor 7/5 para convertir días laborables en días calendario cobrados).
    /// </summary>
    private async Task<int> ComputePermissionChargedMinutesAsync(Permissions p, CancellationToken ct)
    {
        var minutesPerDayStr = await _db.Parameters.AsNoTracking()
            .Where(x => x.Name == "WORK_MINUTES_PER_DAY" && x.IsActive)
            .Select(x => x.Pvalues)
            .FirstOrDefaultAsync(ct);

        var minutesPerDay = int.TryParse(minutesPerDayStr, out var parsed) && parsed > 0 ? parsed : 480;

        var baseMinutes = p.HourTaken.HasValue
            ? (int)p.HourTaken.Value
            : CountWorkingDays(DateOnly.FromDateTime(p.StartDate), DateOnly.FromDateTime(p.EndDate)) * minutesPerDay;

        return (int)Math.Ceiling(baseMinutes * (7.0 / 5.0));
    }

    private static int CountWorkingDays(DateOnly start, DateOnly end)
    {
        var days = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days++;
        }
        return days;
    }

    public async Task<Permissions> CreateWithBalanceCheckAsync(Permissions entity, CancellationToken ct)
    {
        //_logger.LogInformation("**************** accede a CreateWithBalanceCheckAsync");
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        await EnsureNoOverlapAsync(entity.EmployeeId, entity.StartDate, entity.EndDate, ct);

        var strategy = _db.Database.CreateExecutionStrategy();
        Permissions? created = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            entity.HourTaken ??= 0;

            created = await base.CreateAsync(entity, ct);

            _logger.LogInformation(
                "CREATE permission => Id={PermissionId} EmpId={EmpId} ChargedToVacation={Charged} Status={Status}",
                created.PermissionId, created.EmployeeId, created.ChargedToVacation, created.Status
            );

            if (created.ChargedToVacation)
            {
                var reserveSourceId = NewPermissionReserveSourceId(created.PermissionId);
                var chargedMinutes = await ComputePermissionChargedMinutesAsync(created, ct);

                await _balanceAdjustment.ReserveAsync(
                    employeeId: created.EmployeeId,
                    field: TimeBalanceField.Vacation,
                    minutes: chargedMinutes,
                    sourceModule: "PERMISSION_RESERVE",
                    sourceId: reserveSourceId,
                    note: $"Reserva permiso. HourTaken={created.HourTaken} CobradoMin={chargedMinutes} Rango={created.StartDate:yyyy-MM-dd HH:mm} a {created.EndDate:yyyy-MM-dd HH:mm}",
                    performedByEmpId: created.EmployeeId,
                    ct: ct
                );

                _logger.LogInformation(
                    "CREATE permission => ReserveAsync OK. PermissionId={PermissionId} SourceId={SourceId} ChargedMinutes={ChargedMinutes}",
                    created.PermissionId, reserveSourceId, chargedMinutes
                );
            }

            await tx.CommitAsync(ct);
        });

        // CORREO: fuera de la transacción
        if (created is not null)
        {
            _logger.LogInformation("CREATE permission => notificando por correo. PermissionId={PermissionId}", created.PermissionId);
            await NotifyBossOnCreateAsync(created, ct);
        }

        return created!;
    }

    public async Task<Permissions> UpdateBalanceAffectAsync(int id, Permissions entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        var strategy = _db.Database.CreateExecutionStrategy();
        Permissions? updated = null;

        string oldStatus = "";
        string newStatus = "";

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            var current = await _repository.GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"Permissions con id={id} no existe.");

            oldStatus = NormalizeStatus(current.Status);
            newStatus = NormalizeStatus(entity.Status);

            // El aprobador siempre se resuelve del usuario autenticado, nunca del payload del cliente,
            // y no puede aprobar/rechazar su propia solicitud.
            if (newStatus is "APPROVED" or "REJECTED")
            {
                if (_currentUser.EmployeeId is null)
                    throw new BusinessRuleException("No se pudo determinar el empleado autenticado para aprobar/rechazar.");

                if (_currentUser.EmployeeId == current.EmployeeId)
                    throw new BusinessRuleException("No puede aprobar o rechazar su propia solicitud de permiso.");
            }

            await EnsureUpdatePermissionAsync(current, newStatus, ct);

            _logger.LogInformation(
                "UPDATE permission START TraceId={TraceId} PermissionId={PermissionId} OldStatus={OldStatus} NewStatus={NewStatus} OldChargedToVacation={ChargedOld}",
                traceId, id, oldStatus, newStatus, current.ChargedToVacation
            );

            // Aplicar cambios al entity tracked (evita sobrescribir FKs con 0/null no deseados)
            //current.PermissionTypeId = entity.PermissionTypeId;
            //current.StartDate = entity.StartDate;
            //current.EndDate = entity.EndDate;
            //current.ChargedToVacation = entity.ChargedToVacation;
            //current.HourTaken = entity.HourTaken ?? 0;
            current.ApprovedBy = newStatus is "APPROVED" or "REJECTED" ? _currentUser.EmployeeId : entity.ApprovedBy;
            //current.ApprovedAt = entity.ApprovedAt;
            current.ApprovedAt = DateTime.Now;
            current.Justification = entity.Justification ?? "";
            current.Status = entity.Status ?? current.Status;

            // si viene 0 o null, evita romper FK: guarda null si no hay vínculo real
            current.VacationId = entity.VacationId > 0 ? entity.VacationId : null;

            await base.UpdateAsync(id, current, ct);

            updated = await _repository.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException("Error al recargar el permiso actualizado.");

            // Reglas saldo (solo si el permiso afecta vacaciones)
            if (updated.ChargedToVacation)
            {
                bool oldRejected = oldStatus is "REJECTED" or "CANCELED";
                bool newRejected = newStatus is "REJECTED" or "CANCELED";
                bool newPending = newStatus is "PENDING";
                bool newApproved = newStatus is "APPROVED";

                var activeReserveSourceId = await FindActivePermissionReserveSourceIdAsync(id, ct);

                if (!oldRejected && newRejected)
                {
                    if (activeReserveSourceId is not null)
                        await _balanceAdjustment.ReleaseReservationAsync(activeReserveSourceId, performedByEmpId: updated.EmployeeId, ct: ct);
                }

                if (oldRejected && newPending)
                {
                    var chargedMinutes = await ComputePermissionChargedMinutesAsync(updated, ct);

                    await _balanceAdjustment.ReserveAsync(
                        employeeId: updated.EmployeeId,
                        field: TimeBalanceField.Vacation,
                        minutes: chargedMinutes,
                        sourceModule: "PERMISSION_RESERVE",
                        sourceId: NewPermissionReserveSourceId(id),
                        note: $"Reserva permiso (re-planificado). HourTaken={updated.HourTaken} CobradoMin={chargedMinutes}",
                        performedByEmpId: updated.EmployeeId,
                        ct: ct
                    );
                }

                if (!oldRejected && newApproved)
                {
                    if (activeReserveSourceId is not null)
                        await _balanceAdjustment.MarkReservationConsumedAsync(activeReserveSourceId, performedByEmpId: updated.EmployeeId, ct: ct);
                }
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "UPDATE permission COMMIT OK TraceId={TraceId} PermissionId={PermissionId}",
                traceId, id
            );
        });

        // CORREO: fuera de la transacción y solo si cambió el estado
        if (updated is not null && oldStatus != newStatus)
        {
            _logger.LogInformation("STATUS change => disparando notificación. PermissionId={PermissionId} Old={Old} New={New}",
                updated.PermissionId, oldStatus, newStatus);

            await NotifyOnStatusChangedAsync(updated, oldStatus, newStatus, ct);
        }

        return updated!;
    }

    // -----------------------
    // Notificaciones por correo
    // -----------------------

    private async Task NotifyBossOnCreateAsync(Permissions created, CancellationToken ct)
    {
        try
        {
            if (created is null) return;

            await _currentUser.LoadBossAsync(ct);

            var toBoss = _currentUser.BossEmail?.Trim();
            _logger.LogInformation("CREATE permission => BossEmail={BossEmail} PermissionId={PermissionId}", toBoss, created.PermissionId);

            if (string.IsNullOrWhiteSpace(toBoss))
            {
                _logger.LogWarning("CREATE permission => BossEmail vacío. PermissionId={PermissionId}", created.PermissionId);
                return;
            }

            var body = GenerateEmailBodyToApproveSafe(created);
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("CREATE permission => body vacío. PermissionId={PermissionId}", created.PermissionId);
                return;
            }

            await _emailBuilder.TryNotifyAsync(
                EmailTemplateKey.AttendancePunch,
                "Notificación de permiso",
                body,
                to: toBoss,
                timeoutSeconds: 15,
                ct: ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CREATE permission => fallo notificando al jefe. PermissionId={PermissionId}", created?.PermissionId);
        }
    }

    private async Task NotifyOnStatusChangedAsync(Permissions updated, string oldStatus, string newStatus, CancellationToken ct)
    {
        try
        {
            if (updated is null)
            {
                _logger.LogWarning("STATUS change => Notify skipped: updated=null. Old={OldStatus} New={NewStatus}", oldStatus, newStatus);
                return;
            }

            oldStatus = NormalizeStatus(oldStatus);
            newStatus = NormalizeStatus(newStatus);

            _logger.LogInformation("STATUS change => preparando notificación. PermissionId={PermissionId} Old={Old} New={New}",
                updated.PermissionId, oldStatus, newStatus);

            // 1) Si pasó a PENDING: notificar al jefe
            if (newStatus == "PENDING")
            {
                await _currentUser.LoadBossAsync(ct);
                var toBoss = _currentUser.BossEmail?.Trim();

                if (string.IsNullOrWhiteSpace(toBoss))
                {
                    _logger.LogWarning("STATUS change => BossEmail vacío. PermissionId={PermissionId}", updated.PermissionId);
                    return;
                }

                var body = GenerateEmailBodyToApproveSafe(updated);
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("STATUS change => body vacío (to boss). PermissionId={PermissionId}", updated.PermissionId);
                    return;
                }

                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Permiso #{updated.PermissionId} para aprobación",
                    body,
                    to: toBoss,
                    timeoutSeconds: 15,
                    ct: ct
                );

                return;
            }

            // 2) APPROVED/REJECTED/CANCELED: notificar al empleado
            if (newStatus is "APPROVED" or "REJECTED" or "CANCELED")
            {
                var owner = await _employeeDetails.GetEmployeeDetailsAsync(updated.EmployeeId, ct);
                var toEmployee = owner?.Email?.Trim();

                if (string.IsNullOrWhiteSpace(toEmployee))
                {
                    _logger.LogWarning(
                        "STATUS change => email de empleado no disponible. PermissionId={PermissionId} EmployeeId={EmployeeId}",
                        updated.PermissionId, updated.EmployeeId
                    );
                    return;
                }

                var body = GenerateEmailBodyChangeStatusSafe(updated, oldStatus, newStatus);
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("STATUS change => body vacío (to employee). PermissionId={PermissionId}", updated.PermissionId);
                    return;
                }

                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Estado de permiso #{updated.PermissionId}: {newStatus}",
                    body,
                    to: toEmployee,
                    timeoutSeconds: 15,
                    ct: ct
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "STATUS change => fallo notificando. PermissionId={PermissionId} Old={OldStatus} New={NewStatus}",
                updated?.PermissionId, oldStatus, newStatus);
        }
    }

    // -----------------------
    // Helpers / Validaciones / Body
    // -----------------------

    private static void ValidateDates(Permissions entity)
    {
        if (entity.EndDate < entity.StartDate)
            throw new BusinessRuleException("EndDate no puede ser menor que StartDate.");
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "PENDING";
        var v = status.Trim().ToUpperInvariant();
        if (v.Contains("APPROV")) return "APPROVED";
        if (v.Contains("REJECT")) return "REJECTED";
        if (v.Contains("CANCEL")) return "CANCELED";
        if (v.Contains("PEND")) return "PENDING";
        return v;
    }

    private string GenerateEmailBodyToApproveSafe(Permissions permissions)
    {
        try
        {
            return GenerateEmailBodyToApprove(permissions) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email body generation failed (ToApprove). PermissionId={PermissionId}", permissions?.PermissionId);
            return string.Empty;
        }
    }

    private string GenerateEmailBodyChangeStatusSafe(Permissions permissions, string oldStatus, string newStatus)
    {
        try
        {
            return GenerateEmailBodyChangeStatus(permissions, oldStatus, newStatus) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email body generation failed (ChangeStatus). PermissionId={PermissionId}", permissions?.PermissionId);
            return string.Empty;
        }
    }

    private string GenerateEmailBodyToApprove(Permissions permissions)
    {
        var from = permissions.StartDate;
        var to = permissions.EndDate;

        // Evitar NullReference si _currentUser.UserName no está cargado (según tu implementación)
        var requesterName = string.IsNullOrWhiteSpace(_currentUser.UserName)
            ? $"Empleado #{permissions.EmployeeId}"
            : _currentUser.UserName;

        return
            $"<p>Registro de Permiso.</p>" +
            $"<ul>" +
            $"<p>Se ha registrado un permiso para su aprobación</p>" +
            $"<li><b>Empleado:</b> {requesterName}</li>" +
            $"<li><b>Desde:</b> {from:yyyy-MM-dd HH:mm:ss}</li>" +
            $"<li><b>Hasta:</b> {to:yyyy-MM-dd HH:mm:ss}</li>" +
            $"<li><b>Hora Duración:</b> {permissions.HourTaken}</li>" +
            $"</ul>";
    }

    private static string GenerateEmailBodyChangeStatus(Permissions permissions, string oldStatus, string newStatus)
    {
        var from = permissions.StartDate;
        var to = permissions.EndDate;

        return
            $"<p>Estado de Permiso.</p>" +
            $"<ul>" +
            $"<p>El estado del permiso {permissions.PermissionId} cambió de <b>{oldStatus}</b> a <b>{newStatus}</b>.</p>" +
            $"<li><b>Desde:</b> {from:yyyy-MM-dd HH:mm:ss}</li>" +
            $"<li><b>Hasta:</b> {to:yyyy-MM-dd HH:mm:ss}</li>" +
            $"<li><b>Minutos Duración:</b> {permissions.HourTaken}</li>" +
            $"</ul>";
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default)
    {
        var query =
            from perm in _db.Permissions.AsNoTracking()
            join emp  in _db.Employees.AsNoTracking()      on perm.EmployeeId      equals emp.EmployeeId
            join per  in _db.People.AsNoTracking()         on emp.PersonID         equals per.PersonId
            join pt   in _db.PermissionTypes.AsNoTracking() on perm.PermissionTypeId equals pt.TypeId
            join d    in _db.Departments.AsNoTracking()    on emp.DepartmentId     equals d.DepartmentId  into dg
            from d in dg.DefaultIfEmpty()
            join approver in _db.Employees.AsNoTracking()  on perm.ApprovedBy      equals approver.EmployeeId into ag
            from approver in ag.DefaultIfEmpty()
            join approverPerson in _db.People.AsNoTracking() on approver.PersonID  equals approverPerson.PersonId into apg
            from approverPerson in apg.DefaultIfEmpty()
            where (!filter.StartDate.HasValue || perm.StartDate >= filter.StartDate.Value)
               && (!filter.EndDate.HasValue   || perm.StartDate <= filter.EndDate.Value)
               && (string.IsNullOrEmpty(filter.Status) || perm.Status == filter.Status)
               && (!filter.EmployeeId.HasValue || perm.EmployeeId == filter.EmployeeId.Value)
               && (!filter.DepartmentId.HasValue || (d != null && d.DepartmentId == filter.DepartmentId.Value))
            orderby perm.StartDate descending
            select new PermissionReportDto
            {
                PermissionId      = perm.PermissionId,
                PersonIdCard      = per.IdCard,
                PersonFullName    = per.FirstName + " " + per.LastName,
                DepartmentName    = d != null ? d.Name : "—",
                PermissionTypeName = pt.Name,
                StartDate         = perm.StartDate,
                EndDate           = perm.EndDate,
                HourTaken         = perm.HourTaken,
                ChargedToVacation = perm.ChargedToVacation,
                Justification     = perm.Justification,
                Status            = perm.Status,
                ApprovedByName    = approverPerson != null ? approverPerson.FirstName + " " + approverPerson.LastName : null
            };

        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Hueco cerrado 2026-07-22: eliminar (en vez de rechazar/cancelar) un permiso con
    /// cargo a vacaciones y reserva activa dejaba el saldo descontado para siempre — el
    /// DeleteAsync genérico no libera nada. Este método libera la reserva activa (si hay)
    /// antes de borrar la fila.
    /// </summary>
    public async Task DeleteWithBalanceReleaseAsync(int id, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var current = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Permissions con id={id} no existe.");

            if (current.ChargedToVacation)
            {
                var activeReserveSourceId = await FindActivePermissionReserveSourceIdAsync(id, ct);
                if (activeReserveSourceId is not null)
                {
                    await _balanceAdjustment.ReleaseReservationAsync(activeReserveSourceId, performedByEmpId: current.EmployeeId, ct: ct);
                    _logger.LogInformation(
                        "PERM DELETE => reserva liberada antes de borrar. PermissionId={PermissionId} SourceId={SourceId}",
                        id, activeReserveSourceId
                    );
                }
            }

            await base.DeleteAsync(id, ct);

            await tx.CommitAsync(ct);
        });
    }
}
