using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PermissionsRepository : ServiceAwareEfRepository<Permissions, int>, IPermissionsRepository
{
    private readonly DbContext _db;
    public PermissionsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {
        return await _db.Set<Permissions>()
                .Where(rt => rt.EmployeeId == EmployeeId)
                .ToListAsync(ct);
    }

    public async Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
    {
        return await _db.Set<Permissions>()
                .Include(v => v.Employee) // Incluir la relación con Employee
                .Where(v => v.Employee.ImmediateBossId == immediateBossId)
                .ToListAsync(ct);
    }

    public async Task<IEnumerable<Permissions>> GetByImmediateBossIdNonMedical(int immediateBossId, CancellationToken ct)
    {
        return await _db.Set<Permissions>()
            .Include(p => p.Employee)
            .Include(p => p.PermissionType)
            .Where(p =>
                p.Employee.ImmediateBossId == immediateBossId &&
                p.PermissionType.IsActive &&
                !p.PermissionType.IsMedical)
            .ToListAsync(ct);
    }
    public async Task<IEnumerable<Permissions>> GetPendingMedicalPermissions(CancellationToken ct)
    {
        return await _db.Set<Permissions>()
            .Include(p => p.Employee)
            .Include(p => p.PermissionType)
            .Where(p =>                
                p.PermissionType.IsActive &&
                p.PermissionType.IsMedical &&
                p.Status != null &&
                p.Status.Trim().ToLower() == "pending")
            .ToListAsync(ct);
    }

    //public async Task<IEnumerable<Permissions>> GetPendingMedicalPermissions(CancellationToken ct)
    //{
    //    var query =
    //        from p in _db.Set<Permissions>()
    //        join pt in _db.Set<PermissionTypes>() on p.PermissionTypeId equals pt.TypeId
    //        where pt.IsActive
    //              && pt.IsMedical
    //              && p.Status != null
    //              && p.Status.Trim().ToLower() == "pending"
    //        select p;

    //    return await query.ToListAsync(ct);
    //}
}
