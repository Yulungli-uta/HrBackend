using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage; // GetDbTransaction()
using WsUtaSystem.Application.Common;
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

    // ✅ SOLO PARA LOGS (no afecta funcionalidad)
    private readonly ILogger<VacationsService> _logger;

    public VacationsService(
        IVacationsRepository repo,
        ITimeBalancesRepository timeRepo,
        IParametersRepository paramRepo,
        IHrBalanceRepository hrBalanceRepository,
        AppDbContext db,
        ILogger<VacationsService> logger // ✅ agregado
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _timeRepo = timeRepo ?? throw new ArgumentNullException(nameof(timeRepo));
        _paramRepo = paramRepo ?? throw new ArgumentNullException(nameof(paramRepo));
        _hrRepo = hrBalanceRepository ?? throw new ArgumentNullException(nameof(hrBalanceRepository));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // ✅ agregado
    }

    // ✅ IMPORTANTE: Debe coincidir con el SP:
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

            //entity.CreatedAt = entity.CreatedAt == default ? DateTime.Now : entity.CreatedAt;
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

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var adoTx = tx.GetDbTransaction();
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            var current = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Vacations con id={id} no existe.");

            string oldStatus = (current.Status ?? "").Trim().ToUpperInvariant();
            string newStatus = (entity.Status ?? "").Trim().ToUpperInvariant();

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

            // Reglas
            bool oldCanceled = oldStatus is "REJECTED" or "CANCELED";
            bool newCanceled = newStatus is "REJECTED" or "CANCELED";
            bool newPending = newStatus is "PENDING";
            bool newApproved = newStatus is "APPROVED";

            _logger.LogInformation(
                "VAC UPDATE RULES TraceId={TraceId} VacationId={VacationId} oldCanceled={OldCanceled} newCanceled={NewCanceled} newPending={NewPending} newApproved={NewApproved}",
                traceId, id, oldCanceled, newCanceled, newPending, newApproved
            );

            // A) pasa a REJECTED/CANCELED → liberar
            if (!oldCanceled && newCanceled)
            {
                _logger.LogInformation(
                    "VAC REJECT/CANCEL => calling ReleaseReservationAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId} SourceId={SourceId}",
                    traceId, id, updated.EmployeeId, reserveSourceId
                );

                var sp = await _hrRepo.ReleaseReservationAsync(reserveSourceId, updated.EmployeeId, adoTx);

                _logger.LogInformation(
                    "VAC REJECT/CANCEL => ReleaseReservationAsync result TraceId={TraceId} VacationId={VacationId} StatusCode={StatusCode} Ok={Ok} NoOp={NoOp} Message={Message}",
                    traceId, id, sp.StatusCode, sp.Ok, sp.NoOp, sp.Message
                );

                if (!(sp.Ok || sp.NoOp)) throw new BusinessRuleException(sp.Message);
            }

            // B) cancelado/rechazado → vuelve a PENDING → reservar de nuevo
            if (oldCanceled && newPending)
            {
                _logger.LogInformation(
                    "VAC RE-PENDING => calling ReserveVacationAsync TraceId={TraceId} VacationId={VacationId} EmpId={EmpId}",
                    traceId, id, updated.EmployeeId
                );

                var sp = await _hrRepo.ReserveVacationAsync(id, updated.EmployeeId, adoTx);

                _logger.LogInformation(
                    "VAC RE-PENDING => ReserveVacationAsync result TraceId={TraceId} VacationId={VacationId} StatusCode={StatusCode} Ok={Ok} NoOp={NoOp} Message={Message}",
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

        return updated!;
    }
}
