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

    public Task<List<SalaryHistory>> GetAllByActionIdAsync(int actionId, CancellationToken ct) =>
        Query().Where(x => x.ActionId == actionId).ToListAsync(ct);

    // "Último sueldo" por vigencia del documento fuente (ver HR.vw_EmployeeCurrentSalary), no por
    // ChangedAt: ese campo se reescribe en cada edición y un histórico cargado hoy lo adelantaría.
    public Task<SalaryHistory?> GetLatestByEmployeeIdAsync(int employeeId, CancellationToken ct) =>
        _db.CurrentSalaries()
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .FirstOrDefaultAsync(ct);
}
