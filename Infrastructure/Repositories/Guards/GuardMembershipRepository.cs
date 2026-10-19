using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;

namespace WsUtaSystem.Infrastructure.Repositories.Guards;

public class GuardMembershipRepository : IGuardMembershipRepository
{
    private const string GuardJobDescription = "GUARDIAS DE SEGURIDAD";

    private readonly AppDbContext _db;

    public GuardMembershipRepository(AppDbContext db) => _db = db;

    public async Task<bool> IsGuardAsync(int employeeId, DateOnly onDate, CancellationToken ct = default)
    {
        var inRotationGroup = await _db.GuardRotationGroupEmployees
            .AsNoTracking()
            .AnyAsync(g => g.EmployeeId == employeeId
                           && g.IsActive
                           && g.ValidFrom <= onDate
                           && (g.ValidTo == null || g.ValidTo >= onDate), ct);

        if (inRotationGroup) return true;

        return await _db.Employees
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeId == employeeId
                           && e.Job != null
                           && e.Job.Description == GuardJobDescription, ct);
    }
}
