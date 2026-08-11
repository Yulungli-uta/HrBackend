using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage; // GetDbTransaction()
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.TimeBalances;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class VacationsService : Service<Vacations, int>, IVacationsService
{
    private readonly IVacationsRepository _repository;
    private readonly IParametersRepository _paramRepo;
    private readonly IVacationBalanceAdjustmentService _balanceAdjustment;
    private readonly AppDbContext _db;

    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IvwEmployeeDetailsService _employeeDetails;

    private readonly ILogger<VacationsService> _logger;

    public VacationsService(
        IVacationsRepository repo,
        IParametersRepository paramRepo,
        IVacationBalanceAdjustmentService balanceAdjustment,
        AppDbContext db,
        IEmailBuilder emailBuilder,
        ICurrentUserService currentUser,
        IvwEmployeeDetailsService employeeDetails,
        ILogger<VacationsService> logger
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _paramRepo = paramRepo ?? throw new ArgumentNullException(nameof(paramRepo));
        _balanceAdjustment = balanceAdjustment ?? throw new ArgumentNullException(nameof(balanceAdjustment));
        _db = db ?? throw new ArgumentNullException(nameof(db));

        _emailBuilder = emailBuilder ?? throw new ArgumentNullException(nameof(emailBuilder));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails = employeeDetails ?? throw new ArgumentNullException(nameof(employeeDetails));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cada reserva usa un ID único (no uno fijo por VacationID) porque una misma vacación
    /// puede pasar por varios ciclos reserva→libera→reserva (cancelar y volver a
    /// planificar, o cambiar de fechas estando aún Planned/Approved) — con un ID fijo, la
    /// segunda reserva chocaría contra el movimiento de la primera (ya liberado) y se
    /// rechazaría como "reserva duplicada". Hueco cerrado 2026-07-22.
    /// </summary>
    private static string NewVacationReserveSourceId(int vacationId)
        => $"VAC_RESERVE|{vacationId}|{Guid.NewGuid():N}";

    /// <summary>
    /// Encuentra la reserva actualmente activa (ni liberada ni consumida) de esta vacación,
    /// si existe. Necesario porque el sourceId ya no es fijo/predecible por VacationID.
    /// </summary>
    private async Task<string?> FindActiveVacationReserveSourceIdAsync(int vacationId, CancellationToken ct)
    {
        var prefix = $"VAC_RESERVE|{vacationId}|";
        var candidates = await _db.TimeBalanceMovements.AsNoTracking()
            .Where(m => m.SourceModule == "VACATION_RESERVE" && m.SourceID != null && m.SourceID.StartsWith(prefix))
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

    /// <summary>Minutos a reservar por una vacación — mismo cálculo usado en creación y re-planificación.</summary>
    private async Task<int> ComputeChargedMinutesAsync(DateOnly startDate, DateOnly endDate, int daysTaken, CancellationToken ct)
    {
        var daysFromRange = (endDate.DayNumber - startDate.DayNumber) + 1;
        var days = daysTaken > 0 ? daysTaken : daysFromRange;
        var workMinutesPerDay = await GetWorkMinutesPerDayAsync(ct);
        return days * workMinutesPerDay;
    }

    private static readonly string[] ActiveVacationStatuses = { "PLANNED", "INPROGRESS", "APPROVED" };
    private static readonly string[] ActivePermissionStatuses = { "PENDING", "APPROVED" };

    /// <summary>
    /// Bloquea crear una vacación que se superponga con otra vacación activa, o con un
    /// permiso activo, del mismo empleado. Antes solo se validaba en el frontend
    /// (VacationForm.tsx) — el backend no lo rechazaba si se llamaba directo a la API.
    /// </summary>
    private async Task EnsureNoOverlapAsync(int employeeId, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var overlapsVacation = await _db.Vacations.AsNoTracking()
            .Where(v => v.EmployeeId == employeeId
                     && ActiveVacationStatuses.Contains(v.Status.ToUpper())
                     && v.StartDate <= endDate && v.EndDate >= startDate)
            .AnyAsync(ct);

        if (overlapsVacation)
            throw new BusinessRuleException("Ya existe una solicitud de vacaciones activa que se superpone con estas fechas.");

        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        var overlapsPermission = await _db.Permissions.AsNoTracking()
            .Where(p => p.EmployeeId == employeeId
                     && ActivePermissionStatuses.Contains(p.Status.ToUpper())
                     && p.StartDate <= endDateTime && p.EndDate >= startDateTime)
            .AnyAsync(ct);

        if (overlapsPermission)
            throw new BusinessRuleException("Ya existe un permiso activo que se superpone con estas fechas.");
    }

    public async Task<Vacations> CreateWithBalanceCheckAsync(Vacations entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (entity.EndDate < entity.StartDate)
            throw new BusinessRuleException("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        await EnsureNoOverlapAsync(entity.EmployeeId, entity.StartDate, entity.EndDate, ct);

        // Recalcula días por rango si DaysTaken viene mal
        var daysFromRange = (entity.EndDate.DayNumber - entity.StartDate.DayNumber) + 1;
        if (daysFromRange <= 0)
            throw new BusinessRuleException("El rango de fechas es inválido.");

        var days = entity.DaysTaken > 0 ? entity.DaysTaken : daysFromRange;

        var workMinutesPerDay = await GetWorkMinutesPerDayAsync(ct);
        var requestedMinutes = days * workMinutesPerDay;

        // Hueco cerrado 2026-07-22: este pre-check antes leía el saldo vía
        // ITimeBalancesRepository (que elige "el régimen con el ID más bajo"), mientras que
        // la reserva real más abajo usa "el régimen principal" — para un empleado con un
        // solo régimen no había diferencia, pero para uno con 2+ regímenes simultáneos donde
        // el principal no es el de ID más bajo, este pre-check podía validar contra un saldo
        // distinto al que realmente se reserva. Ahora ambos usan la misma resolución.
        var available = await _balanceAdjustment.GetEmployeePrincipalBalanceAsync(entity.EmployeeId, TimeBalanceField.Vacation, ct);

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
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            _logger.LogInformation("VAC CREATE BEGIN TX TraceId={TraceId} EmpId={EmpId}", traceId, entity.EmployeeId);

            entity.CreatedAt = DateTime.Now;

            // 1) Crear (aún sin commit)
            created = await base.CreateAsync(entity, ct);

            var reserveSourceId = NewVacationReserveSourceId(created.VacationId);

            _logger.LogInformation(
                "VAC CREATE created row TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} Status={Status} SourceId={SourceId}",
                traceId, created.VacationId, created.EmployeeId, created.Status, reserveSourceId
            );

            // 2) Reservar saldo (EF, dentro de la misma transacción — participa en ella,
            // ver VacationBalanceAdjustmentService.ApplyAdjustmentAsync)
            _logger.LogInformation(
                "VAC CREATE calling ReserveAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId}",
                traceId, created.VacationId, created.EmployeeId
            );

            await _balanceAdjustment.ReserveAsync(
                employeeId: created.EmployeeId,
                field: TimeBalanceField.Vacation,
                minutes: requestedMinutes,
                sourceModule: "VACATION_RESERVE",
                sourceId: reserveSourceId,
                note: $"Reserva vacaciones. Rango={created.StartDate:yyyy-MM-dd} a {created.EndDate:yyyy-MM-dd} DiasCobrados={days} MinCobrados={requestedMinutes}",
                performedByEmpId: created.EmployeeId,
                ct: ct
            );

            _logger.LogInformation(
                "VAC CREATE ReserveAsync OK TraceId={TraceId} VacationId={VacationId}",
                traceId, created.VacationId
            );

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
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            var current = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Vacations con id={id} no existe.");

            oldStatus = NormalizeStatus(current.Status);
            newStatus = NormalizeStatus(entity.Status);

            // Hueco cerrado 2026-07-22: si cambian fechas/días SIN pasar por una transición
            // de estado que ya recalcula la reserva (A/B abajo), la reserva original queda
            // desalineada del rango real — se detecta aquí, ANTES de aplicar el payload.
            var datesChanged = current.StartDate != entity.StartDate
                             || current.EndDate != entity.EndDate
                             || current.DaysTaken != entity.DaysTaken;

            // El aprobador siempre se resuelve del usuario autenticado (nunca del payload)
            // y no puede aprobar su propia solicitud — mismo criterio que PermissionsService.
            if (newStatus == "APPROVED" && oldStatus != "APPROVED")
            {
                if (_currentUser.EmployeeId == current.EmployeeId)
                    throw new BusinessRuleException("No puede aprobar su propia solicitud de vacaciones.");

                entity.ApprovedBy = _currentUser.EmployeeId;
                entity.ApprovedAt = DateTime.Now;
            }

            entity.UpdatedAt = DateTime.Now;

            // La reserva activa (si hay) ANTES de aplicar el update — el sourceId ya no es
            // fijo por VacationID, se busca por el estado real de los movimientos.
            var activeReserveSourceId = await FindActiveVacationReserveSourceIdAsync(id, ct);

            _logger.LogInformation(
                "VAC UPDATE START TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} OldStatus={OldStatus} NewStatus={NewStatus} DatesChanged={DatesChanged} ActiveReserveSourceId={ActiveReserveSourceId}",
                traceId, id, current.EmployeeId, oldStatus, newStatus, datesChanged, activeReserveSourceId
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
            bool reservationHandled = false;

            _logger.LogInformation(
                "VAC UPDATE RULES TraceId={TraceId} VacationId={VacationId} oldCanceled={OldCanceled} newCanceled={NewCanceled} newPlanned={NewPlanned} newApproved={NewApproved}",
                traceId, id, oldCanceled, newCanceled, newPlanned, newApproved
            );

            // A) pasa a CANCELED → liberar
            if (!oldCanceled && newCanceled)
            {
                _logger.LogInformation(
                    "VAC CANCEL => calling ReleaseReservationAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} SourceId={SourceId}",
                    traceId, id, updated.EmployeeId, activeReserveSourceId
                );

                if (activeReserveSourceId is not null)
                    await _balanceAdjustment.ReleaseReservationAsync(activeReserveSourceId, performedByEmpId: updated.EmployeeId, ct: ct);

                reservationHandled = true;

                _logger.LogInformation(
                    "VAC CANCEL => ReleaseReservationAsync OK TraceId={TraceId} VacationId={VacationId}",
                    traceId, id
                );
            }

            // B) cancelado → vuelve a PLANNED → reservar de nuevo (con las fechas ya actualizadas)
            if (oldCanceled && newPlanned)
            {
                _logger.LogInformation(
                    "VAC RE-PLANNED => calling ReserveAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId}",
                    traceId, id, updated.EmployeeId
                );

                var rePlannedMinutes = await ComputeChargedMinutesAsync(updated.StartDate, updated.EndDate, updated.DaysTaken, ct);

                await _balanceAdjustment.ReserveAsync(
                    employeeId: updated.EmployeeId,
                    field: TimeBalanceField.Vacation,
                    minutes: rePlannedMinutes,
                    sourceModule: "VACATION_RESERVE",
                    sourceId: NewVacationReserveSourceId(id),
                    note: $"Reserva vacaciones (re-planificada). Rango={updated.StartDate:yyyy-MM-dd} a {updated.EndDate:yyyy-MM-dd} MinCobrados={rePlannedMinutes}",
                    performedByEmpId: updated.EmployeeId,
                    ct: ct
                );

                reservationHandled = true;

                _logger.LogInformation(
                    "VAC RE-PLANNED => ReserveAsync OK TraceId={TraceId} VacationId={VacationId}",
                    traceId, id
                );
            }

            // C) pasa a APPROVED → consumir reserva
            if (!oldCanceled && newApproved)
            {
                _logger.LogInformation(
                    "VAC APPROVE => calling MarkReservationConsumedAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} SourceId={SourceId}",
                    traceId, id, updated.EmployeeId, activeReserveSourceId
                );

                if (activeReserveSourceId is not null)
                    await _balanceAdjustment.MarkReservationConsumedAsync(activeReserveSourceId, performedByEmpId: updated.EmployeeId, ct: ct);

                reservationHandled = true;

                _logger.LogInformation(
                    "VAC APPROVE => MarkReservationConsumedAsync OK TraceId={TraceId} VacationId={VacationId}",
                    traceId, id
                );
            }

            // D) Hueco cerrado 2026-07-22: cambiaron fechas/días SIN transición de estado
            // (ej. sigue Planned, o sigue Approved) — sin esto, la reserva original quedaba
            // desalineada del rango real de la vacación (se podía aprobar una vacación más
            // larga de lo que realmente se descontó del saldo). Se libera la reserva vieja
            // (si había) y se reserva de nuevo con el monto correcto para las fechas nuevas.
            if (!reservationHandled && datesChanged && newStatus != "CANCELED")
            {
                _logger.LogInformation(
                    "VAC UPDATE => fechas/días cambiaron sin transición de estado, recalculando reserva. TraceId={TraceId} VacationId={VacationId} PrevActiveSourceId={PrevActiveSourceId}",
                    traceId, id, activeReserveSourceId
                );

                if (activeReserveSourceId is not null)
                    await _balanceAdjustment.ReleaseReservationAsync(activeReserveSourceId, performedByEmpId: updated.EmployeeId, ct: ct);

                var recalculatedMinutes = await ComputeChargedMinutesAsync(updated.StartDate, updated.EndDate, updated.DaysTaken, ct);

                await _balanceAdjustment.ReserveAsync(
                    employeeId: updated.EmployeeId,
                    field: TimeBalanceField.Vacation,
                    minutes: recalculatedMinutes,
                    sourceModule: "VACATION_RESERVE",
                    sourceId: NewVacationReserveSourceId(id),
                    note: $"Reserva vacaciones (recalculada por cambio de fechas). Rango={updated.StartDate:yyyy-MM-dd} a {updated.EndDate:yyyy-MM-dd} MinCobrados={recalculatedMinutes}",
                    performedByEmpId: updated.EmployeeId,
                    ct: ct
                );

                _logger.LogInformation(
                    "VAC UPDATE => reserva recalculada OK. TraceId={TraceId} VacationId={VacationId} NuevoMonto={Minutes}",
                    traceId, id, recalculatedMinutes
                );
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

    /// <summary>
    /// Hueco cerrado 2026-07-22: eliminar (en vez de cancelar) una vacación con reserva
    /// activa dejaba el saldo descontado para siempre — el DeleteAsync genérico no libera
    /// nada. Este método libera la reserva activa (si hay) antes de borrar la fila.
    /// </summary>
    public async Task DeleteWithBalanceReleaseAsync(int id, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var current = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Vacations con id={id} no existe.");

            var activeReserveSourceId = await FindActiveVacationReserveSourceIdAsync(id, ct);
            if (activeReserveSourceId is not null)
            {
                await _balanceAdjustment.ReleaseReservationAsync(activeReserveSourceId, performedByEmpId: current.EmployeeId, ct: ct);
                _logger.LogInformation(
                    "VAC DELETE => reserva liberada antes de borrar. VacationId={VacationId} SourceId={SourceId}",
                    id, activeReserveSourceId
                );
            }

            await base.DeleteAsync(id, ct);

            await tx.CommitAsync(ct);
        });
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
