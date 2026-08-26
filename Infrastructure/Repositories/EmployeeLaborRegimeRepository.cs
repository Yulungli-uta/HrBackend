using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class EmployeeLaborRegimeRepository : ServiceAwareEfRepository<EmployeeLaborRegime, int>, IEmployeeLaborRegimeRepository
{
    private readonly AppDbContext _db;

    public EmployeeLaborRegimeRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<EmployeeLaborRegime>> GetActiveByEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _db.EmployeeLaborRegimes
            .Where(r => r.EmployeeId == employeeId && r.IsActive)
            .ToListAsync(ct);

    public async Task<List<EmployeeLaborRegime>> GetAllByEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _db.EmployeeLaborRegimes
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync(ct);

    public async Task<string?> GetRegimeNameAsync(int laborRegimeId, CancellationToken ct = default)
        => await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.TypeId == laborRegimeId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(ct);

    public async Task<List<int>> GetActiveRegimeIdsByEmployeeIdsAsync(List<int> employeeIds, CancellationToken ct = default)
    {
        if (employeeIds is null || employeeIds.Count == 0) return [];

        return await _db.EmployeeLaborRegimes
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.IsActive)
            .Select(r => r.LaborRegimeId)
            .Distinct()
            .ToListAsync(ct);
    }
}
