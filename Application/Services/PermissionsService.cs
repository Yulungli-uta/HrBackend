using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage; // GetDbTransaction()
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class PermissionsService : Service<Permissions, int>, IPermissionsService
{
    private readonly IPermissionsRepository _repository;
    private readonly IHrBalanceRepository _hrRepo;
    private readonly AppDbContext _db;

    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IvwEmployeeDetailsService _employeeDetails;
    private readonly ILogger<PermissionsService> _logger;

    public PermissionsService(
        IPermissionsRepository repo,
        IHrBalanceRepository hrRepo,
        AppDbContext db,
        IEmailBuilder emailBuilder,
        ICurrentUserService currentUser,
        IvwEmployeeDetailsService employeeDetails,
        ILogger<PermissionsService> logger
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _hrRepo = hrRepo ?? throw new ArgumentNullException(nameof(hrRepo));
        _db = db ?? throw new ArgumentNullException(nameof(db));

        // IMPORTANTE: tu código original tenía esta línea comentada => _emailBuilder quedaba null y causaba NullReference
        _emailBuilder = emailBuilder ?? throw new ArgumentNullException(nameof(emailBuilder));

        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails = employeeDetails ?? throw new ArgumentNullException(nameof(employeeDetails));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string ReserveSourceIdForPermission(int permissionId)
        => $"PERM_RESERVE|{permissionId}";

    public Task<IEnumerable<Permissions>> GetByEmployeeId(int employeeId, CancellationToken ct)
        => _repository.GetByEmployeeId(employeeId, ct);

    public Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
        => _repository.GetByImmediateBossId(immediateBossId, ct);

    public async Task<Permissions> CreateWithBalanceCheckAsync(Permissions entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        var strategy = _db.Database.CreateExecutionStrategy();
        Permissions? created = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var adoTx = tx.GetDbTransaction();

            entity.HourTaken ??= 0;

            created = await base.CreateAsync(entity, ct);

            _logger.LogInformation(
                "CREATE permission => Id={PermissionId} EmpId={EmpId} ChargedToVacation={Charged} Status={Status}",
                created.PermissionId, created.EmployeeId, created.ChargedToVacation, created.Status
            );

            if (created.ChargedToVacation)
            {
                var reserveSourceId = ReserveSourceIdForPermission(created.PermissionId);

                var sp = await _hrRepo.ReservePermissionAsync(
                    permissionId: created.PermissionId,
                    performedByEmpId: created.EmployeeId,
                    tx: adoTx
                );

                _logger.LogInformation(
                    "CREATE permission => ReservePermission SP result. PermissionId={PermissionId} SourceId={SourceId} StatusCode={StatusCode} Message={Message}",
                    created.PermissionId, reserveSourceId, sp.StatusCode, sp.Message
                );

                if (!(sp.Ok || sp.NoOp))
                    throw new BusinessRuleException(sp.Message);
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
            var adoTx = tx.GetDbTransaction();
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            var current = await _repository.GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"Permissions con id={id} no existe.");

            oldStatus = NormalizeStatus(current.Status);
            newStatus = NormalizeStatus(entity.Status);

            _logger.LogInformation(
                "UPDATE permission START TraceId={TraceId} PermissionId={PermissionId} OldStatus={OldStatus} NewStatus={NewStatus} OldChargedToVacation={ChargedOld}",
                traceId, id, oldStatus, newStatus, current.ChargedToVacation
            );

            // Aplicar cambios al entity tracked (evita sobrescribir FKs con 0/null no deseados)
            current.PermissionTypeId = entity.PermissionTypeId;
            current.StartDate = entity.StartDate;
            current.EndDate = entity.EndDate;
            current.ChargedToVacation = entity.ChargedToVacation;
            current.HourTaken = entity.HourTaken ?? 0;
            current.ApprovedBy = entity.ApprovedBy;
            current.ApprovedAt = entity.ApprovedAt;
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

                var reserveSourceId = ReserveSourceIdForPermission(id);

                if (!oldRejected && newRejected)
                {
                    var sp = await _hrRepo.ReleaseReservationAsync(reserveSourceId, updated.EmployeeId, adoTx);
                    if (!(sp.Ok || sp.NoOp)) throw new BusinessRuleException(sp.Message);
                }

                if (oldRejected && newPending)
                {
                    var sp = await _hrRepo.ReservePermissionAsync(id, updated.EmployeeId, adoTx);
                    if (!(sp.Ok || sp.NoOp)) throw new BusinessRuleException(sp.Message);
                }

                if (!oldRejected && newApproved)
                {
                    var sp = await _hrRepo.ConsumeReservationAsync(reserveSourceId, updated.EmployeeId, adoTx);
                    if (!(sp.Ok || sp.NoOp)) throw new BusinessRuleException(sp.Message);
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
}
