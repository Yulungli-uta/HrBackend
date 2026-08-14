using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class SalaryHistoryRepository : ServiceAwareEfRepository<SalaryHistory, int>, ISalaryHistoryRepository
{
    public SalaryHistoryRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public Task<SalaryHistory?> GetByContractIdAsync(int contractId, CancellationToken ct) =>
        Query().Where(x => x.ContractId == contractId).FirstOrDefaultAsync(ct);

    public Task<SalaryHistory?> GetByActionIdAsync(int actionId, CancellationToken ct) =>
        Query().Where(x => x.ActionId == actionId).FirstOrDefaultAsync(ct);

    public Task<SalaryHistory?> GetLatestByEmployeeIdAsync(int employeeId, CancellationToken ct) =>
        Query()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.ChangedAt)
            .ThenByDescending(x => x.SalaryHistoryId)
            .FirstOrDefaultAsync(ct);
}
