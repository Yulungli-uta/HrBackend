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
