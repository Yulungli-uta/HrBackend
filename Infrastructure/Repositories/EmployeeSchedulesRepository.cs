using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmployeeSchedulesRepository : ServiceAwareEfRepository<EmployeeSchedules, int>, IEmployeeSchedulesRepository
{
    private readonly DbContext _db;
    public EmployeeSchedulesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<EmployeeSchedules>> findByEmployeeID(int id, CancellationToken ct)
    {
        //return await _db.Set<EmployeeSchedules>()
        //       .Where(rt => rt.EmployeeId == id )
        //       .ToListAsync(ct);
        //return await _db.Set<EmployeeSchedules>()
        //   .Where(es => es.EmployeeId == id)
        //   .Select(es => new EmployeeSchedules
        //   {
        //       EmpScheduleId = es.EmpScheduleId,
        //       EmployeeId = es.EmployeeId
        //       // Comenta temporalmente las otras propiedades
        //   })
        //   .OrderByDescending(es => es.ValidFrom)
        //   .OrderByDescending(es => es.ValidFrom)
        //   .AsNoTracking()
        //   .ToListAsync(ct);
        var results = await _db.Set<EmployeeSchedules>()
       .Where(es => es.EmployeeId == id)
       .AsNoTracking()
       .ToListAsync(ct);

        // Ordenar en memoria después de cargar los datos
        return results.OrderByDescending(es => es.ValidFrom).AsEnumerable();
    }

    public Task<IEnumerable<EmployeeSchedules>> UpdateEmployeeScheduler(EmployeeSchedules employeeSchedules, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
