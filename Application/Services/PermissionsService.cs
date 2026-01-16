using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage; // GetDbTransaction()
using WsUtaSystem.Application.Common;
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
    private readonly ILogger<PermissionsService> _logger;

    public PermissionsService(
        IPermissionsRepository repo,
        IHrBalanceRepository hrRepo,
        AppDbContext db,
        ILogger<PermissionsService> logger
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _hrRepo = hrRepo ?? throw new ArgumentNullException(nameof(hrRepo));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string ReserveSourceIdForPermission(int permissionId)
        => $"PERM_RESERVE|{permissionId}";

    public async Task<IEnumerable<Permissions>> GetByEmployeeId(int employeeId, CancellationToken ct)
        => await _repository.GetByEmployeeId(employeeId, ct);

    public async Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
        => await _repository.GetByImmediateBossId(immediateBossId, ct);

    public async Task<Permissions> CreateWithBalanceCheckAsync(Permissions entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (entity.EndDate < entity.StartDate)
            throw new BusinessRuleException("EndDate no puede ser menor que StartDate.");

        var strategy = _db.Database.CreateExecutionStrategy();
        Permissions? created = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var adoTx = tx.GetDbTransaction();

            //entity.CreatedAt = entity.CreatedAt == default ? DateTime.Now : entity.CreatedAt;
            entity.CreatedAt = DateTime.Now;

            created = await base.CreateAsync(entity, ct);

            _logger.LogInformation(
                "CREATE permission => Id={PermissionId} EmpId={EmpId} ChargedToVacation={Charged} Status={Status}",
                created.PermissionId, created.EmployeeId, created.ChargedToVacation, created.Status
            );

            if (created.ChargedToVacation)
            {
                var reserveSourceId = ReserveSourceIdForPermission(created.PermissionId);

                _logger.LogInformation(
                    "CREATE permission => calling ReservePermission SP. PermissionId={PermissionId} SourceId={SourceId} TxActive=True",
                    created.PermissionId, reserveSourceId
                );

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

        return created!;
    }

    public async Task<Permissions> UpdateBalanceAffectAsync(int id, Permissions entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (entity.EndDate < entity.StartDate)
            throw new BusinessRuleException("EndDate no puede ser menor que StartDate.");

        var strategy = _db.Database.CreateExecutionStrategy();
        Permissions? updated = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var adoTx = tx.GetDbTransaction();
            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString();

            // 1) Estado anterior
            var current = await _repository.GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"Permissions con id={id} no existe.");

            string oldStatus = (current.Status ?? "").Trim().ToUpperInvariant();
            string newStatus = (entity.Status ?? "").Trim().ToUpperInvariant();

            _logger.LogInformation(
                "UPDATE permission START TraceId={TraceId} PermissionId={PermissionId} OldStatus={OldStatus} NewStatus={NewStatus} OldChargedToVacation={ChargedOld}",
                traceId, id, oldStatus, newStatus, current.ChargedToVacation
            );

            // 2) Actualizar
            await base.UpdateAsync(id, entity, ct);

            // 3) Recargar actualizado
            updated = await _repository.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException("Error al recargar el permiso actualizado.");

            _logger.LogInformation(
                "UPDATE permission AFTER SAVE TraceId={TraceId} PermissionId={PermissionId} StatusNow={StatusNow} ChargedToVacation={ChargedNow} EmpId={EmpId}",
                traceId, id, updated.Status, updated.ChargedToVacation, updated.EmployeeId
            );

            // 4) Reglas saldo (solo si descuenta)
            if (updated.ChargedToVacation)
            {
                bool oldRejected = oldStatus is "REJECTED" or "CANCELED";
                bool newRejected = newStatus is "REJECTED" or "CANCELED";
                bool newPending = newStatus is "PENDING";
                bool newApproved = newStatus is "APPROVED";

                var reserveSourceId = ReserveSourceIdForPermission(id);

                _logger.LogInformation(
                    "UPDATE permission BALANCE RULES TraceId={TraceId} PermissionId={PermissionId} SourceId={SourceId} oldRejected={OldRejected} newRejected={NewRejected} newPending={NewPending} newApproved={NewApproved}",
                    traceId, id, reserveSourceId, oldRejected, newRejected, newPending, newApproved
                );

                // A) → REJECTED/CANCELED = liberar
                if (!oldRejected && newRejected)
                {
                    _logger.LogInformation(
                        "REJECT FLOW => calling ReleaseReservation TraceId={TraceId} PermissionId={PermissionId} EmpId={EmpId} SourceId={SourceId}",
                        traceId, id, updated.EmployeeId, reserveSourceId
                    );

                    var sp = await _hrRepo.ReleaseReservationAsync(reserveSourceId, updated.EmployeeId, adoTx);

                    _logger.LogInformation(
                        "REJECT FLOW => ReleaseReservation result TraceId={TraceId} PermissionId={PermissionId} SourceId={SourceId} StatusCode={StatusCode} Message={Message}",
                        traceId, id, reserveSourceId, sp.StatusCode, sp.Message
                    );

                    if (!(sp.Ok || sp.NoOp))
                        throw new BusinessRuleException(sp.Message);
                }

                // B) REJECTED/CANCELED → PENDING = re-reservar
                if (oldRejected && newPending)
                {
                    _logger.LogInformation(
                        "RE-PENDING FLOW => calling ReservePermission TraceId={TraceId} PermissionId={PermissionId} EmpId={EmpId}",
                        traceId, id, updated.EmployeeId
                    );

                    var sp = await _hrRepo.ReservePermissionAsync(id, updated.EmployeeId, adoTx);

                    _logger.LogInformation(
                        "RE-PENDING FLOW => ReservePermission result TraceId={TraceId} PermissionId={PermissionId} StatusCode={StatusCode} Message={Message}",
                        traceId, id, sp.StatusCode, sp.Message
                    );

                    if (!(sp.Ok || sp.NoOp))
                        throw new BusinessRuleException(sp.Message);
                }

                // C) → APPROVED = consumir (auditoría)
                if (!oldRejected && newApproved)
                {
                    _logger.LogInformation(
                        "APPROVE FLOW => calling ConsumeReservation TraceId={TraceId} PermissionId={PermissionId} EmpId={EmpId} SourceId={SourceId}",
                        traceId, id, updated.EmployeeId, reserveSourceId
                    );

                    var sp = await _hrRepo.ConsumeReservationAsync(reserveSourceId, updated.EmployeeId, adoTx);

                    _logger.LogInformation(
                        "APPROVE FLOW => ConsumeReservation result TraceId={TraceId} PermissionId={PermissionId} SourceId={SourceId} StatusCode={StatusCode} Message={Message}",
                        traceId, id, reserveSourceId, sp.StatusCode, sp.Message
                    );

                    if (!(sp.Ok || sp.NoOp))
                        throw new BusinessRuleException(sp.Message);
                }
            }
            else
            {
                _logger.LogInformation(
                    "UPDATE permission => No balance operations. ChargedToVacation=false. PermissionId={PermissionId}",
                    id
                );
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "UPDATE permission COMMIT OK TraceId={TraceId} PermissionId={PermissionId}",
                traceId, id
            );
        });

        return updated!;
    }
}
