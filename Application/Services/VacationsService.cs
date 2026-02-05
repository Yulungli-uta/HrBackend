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

public class VacationsService : Service<Vacations, int>, IVacationsService
{
    private readonly IVacationsRepository _repository;
    private readonly ITimeBalancesRepository _timeRepo;
    private readonly IParametersRepository _paramRepo;
    private readonly IHrBalanceRepository _hrRepo;
    private readonly AppDbContext _db;

    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IvwEmployeeDetailsService _employeeDetails;

    private readonly ILogger<VacationsService> _logger;

    public VacationsService(
        IVacationsRepository repo,
        ITimeBalancesRepository timeRepo,
        IParametersRepository paramRepo,
        IHrBalanceRepository hrBalanceRepository,
        AppDbContext db,
        IEmailBuilder emailBuilder,
        ICurrentUserService currentUser,
        IvwEmployeeDetailsService employeeDetails,
        ILogger<VacationsService> logger
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _timeRepo = timeRepo ?? throw new ArgumentNullException(nameof(timeRepo));
        _paramRepo = paramRepo ?? throw new ArgumentNullException(nameof(paramRepo));
        _hrRepo = hrBalanceRepository ?? throw new ArgumentNullException(nameof(hrBalanceRepository));
        _db = db ?? throw new ArgumentNullException(nameof(db));

        _emailBuilder = emailBuilder ?? throw new ArgumentNullException(nameof(emailBuilder));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails = employeeDetails ?? throw new ArgumentNullException(nameof(employeeDetails));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // IMPORTANTE: Debe coincidir con el SP:
    // HR.sp_hr_ReserveVacationBalance => SourceID = 'VAC_RESERVE|<VacationID>'
    private static string ReserveSourceIdForVacation(int vacationId)
        => $"VAC_RESERVE|{vacationId}";

    private async Task<int> GetWorkMinutesPerDayAsync(CancellationToken ct)
    {
        var list = await _paramRepo.GetByNameAsync("WORK_MINUTES_PER_DAY", ct);
        var p = list?.FirstOrDefault();

        // Ajusta aquí si el campo se llama distinto
        var v = (p as dynamic)?.Pvalues;

        int minutes = 0;
        try { minutes = Convert.ToInt32(v); } catch { minutes = 0; }

        return minutes > 0 ? minutes : 480;
    }

    public async Task<Vacations> CreateWithBalanceCheckAsync(Vacations entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (entity.EndDate < entity.StartDate)
            throw new BusinessRuleException("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        // Recalcula días por rango si DaysTaken viene mal
        var daysFromRange = (entity.EndDate.DayNumber - entity.StartDate.DayNumber) + 1;
        if (daysFromRange <= 0)
            throw new BusinessRuleException("El rango de fechas es inválido.");

        var days = entity.DaysTaken > 0 ? entity.DaysTaken : daysFromRange;

        var workMinutesPerDay = await GetWorkMinutesPerDayAsync(ct);
        var requestedMinutes = days * workMinutesPerDay;

        var balance = await _timeRepo.GetByIdAsync(entity.EmployeeId, ct);
        var available = balance?.VacationAvailableMin ?? 0;

        _logger.LogInformation(
            "VAC CREATE pre-check: EmpId={EmpId} From={Start} To={End} DaysTaken={DaysTaken} WorkMinDay={WorkMinDay} RequestedMin={RequestedMin} AvailableMin={AvailableMin}",
            entity.EmployeeId, entity.StartDate, entity.EndDate, days, workMinutesPerDay, requestedMinutes, available
        );

        if (requestedMinutes > available)
            throw new BusinessRuleException($"Saldo insuficiente. Disponible: {available} min. Solicitado: {requestedMinutes} min.");

        var strategy = _db.Database.CreateExecutionStrategy();
        Vacations? created = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var adoTx = tx.GetDbTransaction();
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            _logger.LogInformation("VAC CREATE BEGIN TX TraceId={TraceId} EmpId={EmpId}", traceId, entity.EmployeeId);

            entity.CreatedAt = DateTime.Now;

            // 1) Crear (aún sin commit)
            created = await base.CreateAsync(entity, ct);

            var reserveSourceId = ReserveSourceIdForVacation(created.VacationId);

            _logger.LogInformation(
                "VAC CREATE created row TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} Status={Status} SourceId={SourceId}",
                traceId, created.VacationId, created.EmployeeId, created.Status, reserveSourceId
            );

            // 2) Reservar saldo (SP) dentro de la misma transacción
            _logger.LogInformation(
                "VAC CREATE calling ReserveVacationAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId}",
                traceId, created.VacationId, created.EmployeeId
            );

            var sp = await _hrRepo.ReserveVacationAsync(
                vacationId: created.VacationId,
                performedByEmpId: created.EmployeeId,
                tx: adoTx
            );

            _logger.LogInformation(
                "VAC CREATE ReserveVacationAsync result TraceId={TraceId} VacationId={VacationId} StatusCode={StatusCode} Ok={Ok} NoOp={NoOp} Message={Message}",
                traceId, created.VacationId, sp.StatusCode, sp.Ok, sp.NoOp, sp.Message
            );

            if (!(sp.Ok || sp.NoOp))
                throw new BusinessRuleException(sp.Message);

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "VAC CREATE COMMIT OK TraceId={TraceId} VacationId={VacationId}",
                traceId, created.VacationId
            );
        });

        // CORREO: fuera de la transacción
        if (created is not null)
        {
            _logger.LogInformation("VAC CREATE => notificando por correo. VacationId={VacationId}", created.VacationId);
            await NotifyBossOnCreateAsync(created, ct);
        }

        return created!;
    }

    public async Task<IEnumerable<Vacations>> GetByEmployeeId(int employeeId, CancellationToken ct)
        => await _repository.GetByEmployeeId(employeeId, ct);

    public async Task<IEnumerable<Vacations>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
        => await _repository.GetByImmediateBossId(immediateBossId, ct);

    public async Task<Vacations> UpdateBalanceAffectAsync(int id, Vacations entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (entity.EndDate < entity.StartDate)
            throw new BusinessRuleException("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        var strategy = _db.Database.CreateExecutionStrategy();
        Vacations? updated = null;

        // Para notificación fuera de TX
        string oldStatus = "";
        string newStatus = "";

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var adoTx = tx.GetDbTransaction();
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            var current = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Vacations con id={id} no existe.");

            oldStatus = NormalizeStatus(current.Status);
            newStatus = NormalizeStatus(entity.Status);

            var reserveSourceId = ReserveSourceIdForVacation(id);

            entity.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "VAC UPDATE START TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} OldStatus={OldStatus} NewStatus={NewStatus} SourceId={SourceId}",
                traceId, id, current.EmployeeId, oldStatus, newStatus, reserveSourceId
            );

            await base.UpdateAsync(id, entity, ct);

            updated = await _repository.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException("Error al recargar la vacación actualizada.");

            _logger.LogInformation(
                "VAC UPDATE AFTER SAVE TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} StatusNow={StatusNow}",
                traceId, id, updated.EmployeeId, updated.Status
            );

            // ----------------------------
            // Reglas de saldo alineadas a:
            // Planned | InProgress | Approved | Canceled | Completed
            // ----------------------------
            bool oldCanceled = oldStatus == "CANCELED";
            bool newCanceled = newStatus == "CANCELED";
            bool newPlanned = newStatus == "PLANNED";
            bool newApproved = newStatus == "APPROVED";

            _logger.LogInformation(
                "VAC UPDATE RULES TraceId={TraceId} VacationId={VacationId} oldCanceled={OldCanceled} newCanceled={NewCanceled} newPlanned={NewPlanned} newApproved={NewApproved}",
                traceId, id, oldCanceled, newCanceled, newPlanned, newApproved
            );

            // A) pasa a CANCELED → liberar
            if (!oldCanceled && newCanceled)
            {
                _logger.LogInformation(
                    "VAC CANCEL => calling ReleaseReservationAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} SourceId={SourceId}",
                    traceId, id, updated.EmployeeId, reserveSourceId
                );

                var sp = await _hrRepo.ReleaseReservationAsync(reserveSourceId, updated.EmployeeId, adoTx);

                _logger.LogInformation(
                    "VAC CANCEL => ReleaseReservationAsync result TraceId={TraceId} VacationId={VacationId} StatusCode={StatusCode} Ok={Ok} NoOp={NoOp} Message={Message}",
                    traceId, id, sp.StatusCode, sp.Ok, sp.NoOp, sp.Message
                );

                if (!(sp.Ok || sp.NoOp)) throw new BusinessRuleException(sp.Message);
            }

            // B) cancelado → vuelve a PLANNED → reservar de nuevo
            if (oldCanceled && newPlanned)
            {
                _logger.LogInformation(
                    "VAC RE-PLANNED => calling ReserveVacationAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId}",
                    traceId, id, updated.EmployeeId
                );

                var sp = await _hrRepo.ReserveVacationAsync(id, updated.EmployeeId, adoTx);

                _logger.LogInformation(
                    "VAC RE-PLANNED => ReserveVacationAsync result TraceId={TraceId} VacationId={VacationId} StatusCode={StatusCode} Ok={Ok} NoOp={NoOp} Message={Message}",
                    traceId, id, sp.StatusCode, sp.Ok, sp.NoOp, sp.Message
                );

                if (!(sp.Ok || sp.NoOp)) throw new BusinessRuleException(sp.Message);
            }

            // C) pasa a APPROVED → consumir reserva
            if (!oldCanceled && newApproved)
            {
                _logger.LogInformation(
                    "VAC APPROVE => calling ConsumeReservationAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} SourceId={SourceId}",
                    traceId, id, updated.EmployeeId, reserveSourceId
                );

                var sp = await _hrRepo.ConsumeReservationAsync(reserveSourceId, updated.EmployeeId, adoTx);

                _logger.LogInformation(
                    "VAC APPROVE => ConsumeReservationAsync result TraceId={TraceId} VacationId={VacationId} StatusCode={StatusCode} Ok={Ok} NoOp={NoOp} Message={Message}",
                    traceId, id, sp.StatusCode, sp.Ok, sp.NoOp, sp.Message
                );

                if (!(sp.Ok || sp.NoOp)) throw new BusinessRuleException(sp.Message);
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "VAC UPDATE COMMIT OK TraceId={TraceId} VacationId={VacationId}",
                traceId, id
            );
        });

        // CORREO: fuera de la transacción y solo si cambió el estado
        if (updated is not null && !string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "VAC STATUS change => disparando notificación. VacationId={VacationId} Old={Old} New={New}",
                updated.VacationId, oldStatus, newStatus
            );

            await NotifyOnStatusChangedAsync(updated, oldStatus, newStatus, ct);
        }

        return updated!;
    }

    // -----------------------
    // Notificaciones por correo
    // -----------------------

    private async Task NotifyBossOnCreateAsync(Vacations created, CancellationToken ct)
    {
        try
        {
            if (created is null) return;

            await _currentUser.LoadBossAsync(ct);

            var toBoss = _currentUser.BossEmail?.Trim();
            _logger.LogInformation("VAC CREATE => BossEmail={BossEmail} VacationId={VacationId}", toBoss, created.VacationId);

            if (string.IsNullOrWhiteSpace(toBoss))
            {
                _logger.LogWarning("VAC CREATE => BossEmail vacío. VacationId={VacationId}", created.VacationId);
                return;
            }

            var body = GenerateEmailBodyToApproveSafe(created);
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("VAC CREATE => body vacío. VacationId={VacationId}", created.VacationId);
                return;
            }

            await _emailBuilder.TryNotifyAsync(
                EmailTemplateKey.AttendancePunch,
                $"Vacaciones #{created.VacationId} para aprobación",
                body,
                to: toBoss,
                timeoutSeconds: 15,
                ct: ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VAC CREATE => fallo notificando al jefe. VacationId={VacationId}", created?.VacationId);
        }
    }

    private async Task NotifyOnStatusChangedAsync(Vacations updated, string oldStatus, string newStatus, CancellationToken ct)
    {
        try
        {
            if (updated is null)
            {
                _logger.LogWarning("VAC STATUS change => Notify skipped: updated=null. Old={OldStatus} New={NewStatus}", oldStatus, newStatus);
                return;
            }

            oldStatus = NormalizeStatus(oldStatus);
            newStatus = NormalizeStatus(newStatus);

            _logger.LogInformation(
                "VAC STATUS change => preparando notificación. VacationId={VacationId} Old={Old} New={New}",
                updated.VacationId, oldStatus, newStatus
            );

            // 1) Si pasa a PLANNED: notificar al jefe (solicitud / re-solicitud)
            if (newStatus == "PLANNED")
            {
                await _currentUser.LoadBossAsync(ct);
                var toBoss = _currentUser.BossEmail?.Trim();

                if (string.IsNullOrWhiteSpace(toBoss))
                {
                    _logger.LogWarning("VAC STATUS change => BossEmail vacío. VacationId={VacationId}", updated.VacationId);
                    return;
                }

                var body = GenerateEmailBodyToApproveSafe(updated);
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("VAC STATUS change => body vacío (to boss). VacationId={VacationId}", updated.VacationId);
                    return;
                }

                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Vacaciones #{updated.VacationId} para aprobación",
                    body,
                    to: toBoss,
                    timeoutSeconds: 15,
                    ct: ct
                );

                return;
            }

            // 2) Approved / Canceled / InProgress / Completed: notificar al empleado
            if (newStatus is "APPROVED" or "CANCELED" or "INPROGRESS" or "COMPLETED")
            {
                var owner = await _employeeDetails.GetEmployeeDetailsAsync(updated.EmployeeId, ct);
                var toEmployee = owner?.Email?.Trim();

                if (string.IsNullOrWhiteSpace(toEmployee))
                {
                    _logger.LogWarning(
                        "VAC STATUS change => email de empleado no disponible. VacationId={VacationId} EmployeeId={EmployeeId}",
                        updated.VacationId, updated.EmployeeId
                    );
                    return;
                }

                var body = GenerateEmailBodyChangeStatusSafe(updated, oldStatus, newStatus);
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("VAC STATUS change => body vacío (to employee). VacationId={VacationId}", updated.VacationId);
                    return;
                }

                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Estado de vacaciones #{updated.VacationId}: {ToDbStatusTitleCase(newStatus)}",
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
                "VAC STATUS change => fallo notificando. VacationId={VacationId} Old={OldStatus} New={NewStatus}",
                updated?.VacationId, oldStatus, newStatus);
        }
    }

    // -----------------------
    // Helpers / Status / Body
    // -----------------------

    // Normaliza hacia los estados válidos en BD:
    // Planned | InProgress | Approved | Canceled | Completed
    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "PLANNED";

        var v = status.Trim().ToUpperInvariant();

        // Permite variantes comunes (por si llegan desde UI/legacy)
        if (v.Contains("PLAN")) return "PLANNED";
        if (v.Contains("INPROG") || v.Contains("IN_PROGRESS") || v.Contains("IN PROGRESS")) return "INPROGRESS";
        if (v.Contains("APPROV")) return "APPROVED";
        if (v.Contains("CANCEL")) return "CANCELED";
        if (v.Contains("COMPLET")) return "COMPLETED";

        // Si llega exacto, lo mapeamos
        return v switch
        {
            "PLANNED" => "PLANNED",
            "INPROGRESS" => "INPROGRESS",
            "APPROVED" => "APPROVED",
            "CANCELED" => "CANCELED",
            "COMPLETED" => "COMPLETED",
            _ => "PLANNED"
        };
    }

    private static string ToDbStatusTitleCase(string normalized)
        => normalized switch
        {
            "PLANNED" => "Planned",
            "INPROGRESS" => "InProgress",
            "APPROVED" => "Approved",
            "CANCELED" => "Canceled",
            "COMPLETED" => "Completed",
            _ => "Planned"
        };

    private string GenerateEmailBodyToApproveSafe(Vacations vacations)
    {
        try
        {
            return GenerateEmailBodyToApprove(vacations) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VAC Email body generation failed (ToApprove). VacationId={VacationId}", vacations?.VacationId);
            return string.Empty;
        }
    }

    private string GenerateEmailBodyChangeStatusSafe(Vacations vacations, string oldStatus, string newStatus)
    {
        try
        {
            return GenerateEmailBodyChangeStatus(vacations, oldStatus, newStatus) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VAC Email body generation failed (ChangeStatus). VacationId={VacationId}", vacations?.VacationId);
            return string.Empty;
        }
    }

    private string GenerateEmailBodyToApprove(Vacations vacations)
    {
        var from = vacations.StartDate;
        var to = vacations.EndDate;

        var requesterName = string.IsNullOrWhiteSpace(_currentUser.UserName)
            ? $"Empleado #{vacations.EmployeeId}"
            : _currentUser.UserName;

        return
            $"<p>Registro de Vacaciones.</p>" +
            $"<ul>" +
            $"<p>Se ha registrado una solicitud de vacaciones para su aprobación</p>" +
            $"<li><b>Empleado:</b> {requesterName}</li>" +
            $"<li><b>Desde:</b> {from:yyyy-MM-dd}</li>" +
            $"<li><b>Hasta:</b> {to:yyyy-MM-dd}</li>" +
            $"<li><b>Días:</b> {vacations.DaysTaken}</li>" +
            $"</ul>";
    }

    private static string GenerateEmailBodyChangeStatus(Vacations vacations, string oldStatus, string newStatus)
    {
        var from = vacations.StartDate;
        var to = vacations.EndDate;

        return
            $"<p>Estado de Vacaciones.</p>" +
            $"<ul>" +
            $"<p>El estado de la solicitud #{vacations.VacationId} cambió de <b>{ToDbStatusTitleCase(oldStatus)}</b> a <b>{ToDbStatusTitleCase(newStatus)}</b>.</p>" +
            $"<li><b>Desde:</b> {from:yyyy-MM-dd}</li>" +
            $"<li><b>Hasta:</b> {to:yyyy-MM-dd}</li>" +
            $"<li><b>Días:</b> {vacations.DaysTaken}</li>" +
            $"</ul>";
    }
}
