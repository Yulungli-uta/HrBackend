using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class SalaryHistoryService : Service<SalaryHistory, int>, ISalaryHistoryService
{
    private readonly ISalaryHistoryRepository _repository;

    public SalaryHistoryService(ISalaryHistoryRepository repo) : base(repo)
    {
        _repository = repo;
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
            existing.ChangedAt = DateTime.UtcNow;
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
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        }, ct);
    }

    /// <inheritdoc/>
    public async Task UpsertForActionAsync(
        int actionId, int employeeId, decimal previousSalary, decimal newSalary,
        string changedBy, string? reason, CancellationToken ct)
    {
        var existing = await _repository.GetByActionIdAsync(actionId, ct);
        if (existing is not null)
        {
            existing.OldSalary = previousSalary;
            existing.NewSalary = newSalary;
            existing.EmployeeId = employeeId;
            existing.ChangedBy = changedBy;
            existing.ChangedAt = DateTime.UtcNow;
            existing.Reason = reason;
            await _repository.UpdateAsync(existing.SalaryHistoryId, existing, ct);
            return;
        }

        await _repository.AddAsync(new SalaryHistory
        {
            ActionId = actionId,
            EmployeeId = employeeId,
            OldSalary = previousSalary,
            NewSalary = newSalary,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        }, ct);
    }
}
