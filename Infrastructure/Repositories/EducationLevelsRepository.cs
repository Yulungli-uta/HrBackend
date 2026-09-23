using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Views;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EducationLevelsRepository : ServiceAwareEfRepository<EducationLevels, int>, IEducationLevelsRepository
{

    private readonly DbContext _db;
    public EducationLevelsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<EducationLevels>().Where(e => e.PersonId == personId).ToListAsync();
    }

    public async Task<IReadOnlyList<(int EmployeeId, int? DepartmentId, string? Nivel, string? Grado)>> GetActiveProfessorStatsAsync(CancellationToken ct = default)
    {
        var query = _db.Set<VwSiiesFormacionProfesional>()
            .Join(_db.Set<Employees>(), v => v.EmployeeID, e => e.EmployeeId, (v, e) => new { v, e })
            .Where(x => x.e.IsActive)
            .Select(x => new { x.e.EmployeeId, x.e.DepartmentId, x.v.NivelSiiesLabel, x.v.GradoSiiesLabel });

        var rows = await query.ToListAsync(ct);
        return rows.Select(r => (r.EmployeeId, r.DepartmentId, r.NivelSiiesLabel, r.GradoSiiesLabel)).ToList();
    }

}