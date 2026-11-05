using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class SalaryHistoryService : Service<SalaryHistory, int>, ISalaryHistoryService
{
    private readonly ISalaryHistoryRepository _repository;
    private readonly AppDbContext _db;

    private readonly IAppClock _clock;

    public SalaryHistoryService(ISalaryHistoryRepository repo, AppDbContext db, IAppClock clock) : base(repo)
    {
        _clock = clock;
        _repository = repo;
        _db = db;
    }

    /// <inheritdoc/>
    public async Task UpsertForContractAsync(
        int contractId, int employeeId, decimal newSalary,
        string changedBy, string? reason, CancellationToken ct)
    {
        var existing = await _repository.GetByContractIdAsync(contractId, ct);
        if (existing is not null)
        {
            existing.NewSalary = newSalary;
            existing.EmployeeId = employeeId;
            existing.ChangedBy = changedBy;
            existing.ChangedAt = _clock.Now;
            existing.Reason = reason;
            await _repository.UpdateAsync(existing.SalaryHistoryId, existing, ct);
            return;
        }

        var previous = await _repository.GetLatestByEmployeeIdAsync(employeeId, ct);

        await _repository.AddAsync(new SalaryHistory
        {
            ContractId = contractId,
            EmployeeId = employeeId,
            OldSalary = previous?.NewSalary ?? newSalary,
            NewSalary = newSalary,
            ChangedBy = changedBy,
            ChangedAt = _clock.Now,
            Reason = reason
        }, ct);
    }

    /// <inheritdoc/>
    public async Task UpsertForActionAsync(
        int actionId, int employeeId, decimal? previousSalary, decimal newSalary,
        string changedBy, string? reason, CancellationToken ct)
    {
        // Todas las filas de la acción, no una arbitraria: la migración de 2022 dejó acciones con
        // dos filas (sueldos distintos); corregir la acción las deja consistentes entre sí.
        var existing = await _repository.GetAllByActionIdAsync(actionId, ct);
        if (existing.Count > 0)
        {
            foreach (var row in existing)
            {
                // Si no se informó RMU Origen, se conserva el OldSalary ya guardado en vez de
                // pisarlo con 0 (antes `previousRmu ?? 0` borraba un dato válido).
                if (previousSalary.HasValue)
                    row.OldSalary = previousSalary.Value;
                row.NewSalary = newSalary;
                row.EmployeeId = employeeId;
                row.ChangedBy = changedBy;
                row.ChangedAt = _clock.Now;
                row.Reason = reason;
                await _repository.UpdateAsync(row.SalaryHistoryId, row, ct);
            }
            return;
        }

        // Fila nueva sin RMU Origen: mismo criterio que UpsertForContractAsync (último sueldo
        // conocido del empleado; si no hay, el nuevo), nunca 0.
        var previous = previousSalary
            ?? (await _repository.GetLatestByEmployeeIdAsync(employeeId, ct))?.NewSalary
            ?? newSalary;

        await _repository.AddAsync(new SalaryHistory
        {
            ActionId = actionId,
            EmployeeId = employeeId,
            OldSalary = previous,
            NewSalary = newSalary,
            ChangedBy = changedBy,
            ChangedAt = _clock.Now,
            Reason = reason
        }, ct);
    }

    /// <inheritdoc/>
    public async Task<SalaryHistory> CreateManualAsync(SalaryHistory entity, string changedBy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var reason = entity.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5)
            throw new InvalidOperationException("Debe indicar el motivo del registro (mínimo 5 caracteres).");
        if (entity.NewSalary <= 0)
            throw new InvalidOperationException("El sueldo debe ser mayor a 0.");
        if (!entity.EmployeeId.HasValue)
            throw new InvalidOperationException("Indique el empleado al que pertenece el sueldo.");
        if (entity.ActionId.HasValue == entity.ContractId.HasValue)
            throw new InvalidOperationException("Indique exactamente un documento fuente: una acción de personal o un contrato.");

        var employeeId = entity.EmployeeId.Value;

        if (entity.ActionId.HasValue)
        {
            var action = await _db.PersonnelActions.AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActionId == entity.ActionId.Value && !a.IsDeleted, ct)
                ?? throw new KeyNotFoundException($"La acción de personal {entity.ActionId} no existe.");
            if (action.EmployeeId != employeeId)
                throw new InvalidOperationException("La acción de personal indicada no pertenece a ese empleado.");
            if (await _repository.GetByActionIdAsync(entity.ActionId.Value, ct) is not null)
                throw new InvalidOperationException(
                    "Esa acción ya tiene su sueldo en el libro. Para cambiarlo, corrija la acción de personal desde su pantalla de Corrección.");
        }
        else
        {
            var contractId = entity.ContractId!.Value;
            var contract = await _db.Contracts.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContractID == contractId, ct)
                ?? throw new KeyNotFoundException($"El contrato {contractId} no existe.");
            var belongs = await _db.Employees.AsNoTracking()
                .AnyAsync(e => e.EmployeeId == employeeId && e.PersonID == contract.PersonID, ct);
            if (!belongs)
                throw new InvalidOperationException("El contrato indicado no pertenece a ese empleado.");
            if (await _repository.GetByContractIdAsync(contractId, ct) is not null)
                throw new InvalidOperationException(
                    "Ese contrato ya tiene su sueldo en el libro. Para cambiarlo, corrija el contrato desde su pantalla de Corrección.");
        }

        var previous = await _repository.GetLatestByEmployeeIdAsync(employeeId, ct);

        // Usuario, fecha y sueldo anterior los decide el servidor, nunca el cliente.
        entity.SalaryHistoryId = 0;
        entity.Reason          = reason;
        entity.ChangedBy       = changedBy;
        entity.ChangedAt       = _clock.Now;
        entity.OldSalary       = previous?.NewSalary ?? entity.NewSalary;

        await _repository.AddAsync(entity, ct);
        return entity;
    }

    /// <summary>
    /// Toda fila del libro depende de un documento fuente (CK_SalaryHistory_HasSource), así que editarla directamente
    /// dejaría el documento y el libro en desacuerdo. El cambio correcto es corregir el documento.
    /// </summary>
    public override Task UpdateAsync(int id, SalaryHistory entity, CancellationToken ct) =>
        throw new InvalidOperationException(
            "No se puede editar una fila del libro de sueldos directamente: corrija el documento fuente (acción de personal o contrato) desde su pantalla de Corrección.");

    /// <summary>Misma razón que <see cref="UpdateAsync"/>: borrar una fila dejaría un documento sin su sueldo registrado.</summary>
    public override Task DeleteAsync(int id, CancellationToken ct) =>
        throw new InvalidOperationException(
            "No se puede eliminar una fila del libro de sueldos: corrija o anule el documento fuente (acción de personal o contrato).");

    public async Task<decimal?> GetReferenceRmuAsync(int? employeeId, int? jobId, CancellationToken ct = default)
    {
        if (!employeeId.HasValue && !jobId.HasValue)
            return null;

        if (employeeId.HasValue)
        {
            var latest = await _repository.GetLatestByEmployeeIdAsync(employeeId.Value, ct);
            if (latest is not null)
                return latest.NewSalary;

            // Sin historial real (empleado nuevo): respaldo por el cargo actual, si no vino uno explícito.
            jobId ??= await _db.Employees.AsNoTracking()
                .Where(e => e.EmployeeId == employeeId.Value)
                .Select(e => (int?)e.JobId)
                .FirstOrDefaultAsync(ct);
        }

        if (!jobId.HasValue || jobId.Value == 0)
            return null;

        var result = await (
            from j in _db.Jobs.AsNoTracking()
            where j.JobID == jobId.Value
            join og in _db.OccupationalGroup.AsNoTracking() on j.GroupId equals og.GroupId into ogJoin
            from og in ogJoin.DefaultIfEmpty()
            select new { j.ReferenceSalary, GroupRmu = (decimal?)og.Rmu }
        ).FirstOrDefaultAsync(ct);

        return result?.ReferenceSalary ?? result?.GroupRmu;
    }
}
