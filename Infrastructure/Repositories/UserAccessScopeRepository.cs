using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class UserAccessScopeRepository : ServiceAwareEfRepository<UserAccessScope, int>, IUserAccessScopeRepository
{
    private const string ModuleCategory = "ACCESS_MODULE_TYPE";

    private readonly AppDbContext _db;

    public UserAccessScopeRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<UserAccessScope>> GetActiveByEmployeeAndModuleAsync(
        int employeeId, string moduleCode, CancellationToken ct = default)
    {
        var now = DateTime.Now;

        var moduleTypeId = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == ModuleCategory && r.Name == moduleCode && r.IsActive)
            .Select(r => r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (moduleTypeId == 0) return [];

        return await _db.UserAccessScopes
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId
                     && s.ModuleTypeId == moduleTypeId
                     && s.IsActive
                     && (s.ExpiresAt == null || s.ExpiresAt > now))
            .Include(s => s.ScopeType)
            .ToListAsync(ct);
    }

    public async Task<List<int>> GetDepartmentTreeIdsAsync(int departmentId, CancellationToken ct = default)
    {
        // Carga liviana de (Id, ParentId) para resolver el árbol en memoria,
        // evitando CTE recursivos específicos de SQL Server en EF Core.
        var all = await _db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new { d.DepartmentId, d.ParentId })
            .ToListAsync(ct);

        var childrenByParent = all
            .Where(d => d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DepartmentId).ToList());

        var result = new List<int> { departmentId };
        var queue = new Queue<int>();
        queue.Enqueue(departmentId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children)) continue;

            foreach (var childId in children)
            {
                if (result.Contains(childId)) continue;
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    public async Task<List<UserAccessScopeHistory>> GetHistoryByEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await _db.UserAccessScopeHistory
            .AsNoTracking()
            .Where(h => h.EmployeeId == employeeId)
            .OrderByDescending(h => h.ChangeDateTime)
            .ToListAsync(ct);

    public async Task AddHistoryAsync(UserAccessScopeHistory history, CancellationToken ct = default)
    {
        _db.UserAccessScopeHistory.Add(history);
        await _db.SaveChangesAsync(ct);
    }
}
