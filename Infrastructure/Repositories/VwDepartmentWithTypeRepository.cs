using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories;

public class VwDepartmentWithTypeRepository : IVwDepartmentWithTypeRepository
{
    private readonly AppDbContext _db;

    public VwDepartmentWithTypeRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<VwDepartmentWithType>> GetAllAsync(CancellationToken ct = default) =>
        await _db.VwDepartmentWithType.AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<VwDepartmentWithType>> GetActiveAsync(CancellationToken ct = default) =>
        await _db.VwDepartmentWithType.AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(ct);

    public async Task<IEnumerable<VwDepartmentWithType>> GetByTypeAsync(int departmentTypeId, CancellationToken ct = default) =>
        await _db.VwDepartmentWithType.AsNoTracking()
            .Where(d => d.DepartmentTypeID == departmentTypeId)
            .ToListAsync(ct);

    public async Task<IEnumerable<VwDepartmentWithType>> GetByScopeAsync(int departmentScopeId, CancellationToken ct = default) =>
        await _db.VwDepartmentWithType.AsNoTracking()
            .Where(d => d.DepartmentScopeID == departmentScopeId)
            .ToListAsync(ct);

    public async Task<VwDepartmentWithType?> GetByIdAsync(int departmentId, CancellationToken ct = default) =>
        await _db.VwDepartmentWithType.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentID == departmentId, ct);
}
