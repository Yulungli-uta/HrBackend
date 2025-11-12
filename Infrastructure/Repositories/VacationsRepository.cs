using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class VacationsRepository : ServiceAwareEfRepository<Vacations, int>, IVacationsRepository
{
    private readonly DbContext _db;
    public VacationsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<Vacations>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {
        return await _db.Set<Vacations>()
                .Where(rt => rt.EmployeeId == EmployeeId)
                .ToListAsync(ct);
    }

    public async Task<IEnumerable<Vacations>> GetByImmediateBossId(int immediateBossId, CancellationToken ct)
    {
        return await _db.Set<Vacations>()
                .Include(v => v.Employee) // Incluir la relación con Employee
                .Where(v => v.Employee.ImmediateBossId == immediateBossId)
                .ToListAsync(ct);
    }
}
