using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories;

public class VwEmployeePositionRepository : IVwEmployeePositionRepository
{
    private readonly AppDbContext _db;

    public VwEmployeePositionRepository(AppDbContext db) => _db = db;

    /// <inheritdoc/>
    public Task<List<VwEmployeePosition>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default) =>
        _db.VwEmployeePositions.AsNoTracking().Where(p => p.EmployeeID == employeeId).OrderBy(p => p.PositionType).ToListAsync(ct);

    /// <inheritdoc/>
    public Task<List<VwEmployeePosition>> GetByEmployeesAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default) =>
        _db.VwEmployeePositions.AsNoTracking().Where(p => employeeIds.Contains(p.EmployeeID)).ToListAsync(ct);
}
