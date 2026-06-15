using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class TeacherStructureRepository : ServiceAwareEfRepository<TeacherStructure, int>, ITeacherStructureRepository
{
    public TeacherStructureRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<List<TeacherStructure>> GetByEmployeeAsync(int employeeId, CancellationToken ct) =>
        await _db.TeacherStructures
            .Include(t => t.Ladder)
            .Include(t => t.DedicationType)
            .Include(t => t.Department)
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync(ct);

    public async Task<bool> HasOverlapAsync(int employeeId, DateOnly startDate, DateOnly? endDate, int? excludeId, CancellationToken ct)
    {
        var query = _db.TeacherStructures
            .Where(t => t.EmployeeId == employeeId && t.IsActive && (excludeId == null || t.TeacherStructureId != excludeId));

        // Solapamiento: el nuevo período se cruza con alguno existente
        return await query.AnyAsync(t =>
            t.StartDate < (endDate ?? DateOnly.MaxValue) &&
            (t.EndDate == null || t.EndDate > startDate), ct);
    }
}
