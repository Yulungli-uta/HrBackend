using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace WsUtaSystem.Infrastructure.Repositories;
public class AttendancePunchesRepository : ServiceAwareEfRepository<AttendancePunches, int>, IAttendancePunchesRepository
{
    private readonly DbContext _db;
    public AttendancePunchesRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
    {
        _db = db;
    }

    //public async Task<AttendancePunches> GetLastPunchAsync(int employeeId, CancellationToken ct)
    //{
    //    return await _db.Set<AttendancePunches>()
    //        .Where(ap => ap.EmployeeId == employeeId)
    //        .OrderByDescending(ap => ap.PunchTime)
    //        .FirstOrDefaultAsync(ct);
    //}

    public async Task<IEnumerable<AttendancePunches>> GetTodayPunchesByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        //var today = DateTime.Today;
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _db.Set<AttendancePunches>()
            .Where(ap => ap.EmployeeId == employeeId &&
                         ap.PunchTime >= today &&
                         ap.PunchTime < tomorrow)
            .OrderBy(ap => ap.PunchTime)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetPunchesByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var startOfDay = startDate.Date; 
        var endOfDay = endDate.Date.AddDays(1); 

        return await _db.Set<AttendancePunches>()
            .Where(ap => ap.EmployeeId == employeeId &&
                         ap.PunchTime >= startOfDay &&
                         ap.PunchTime < endOfDay) 
            .OrderBy(ap => ap.PunchTime)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetPunchesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await _db.Set<AttendancePunches>()
            .Where(ap => ap.PunchTime >= startDate && ap.PunchTime <= endDate)
            .OrderBy(ap => ap.PunchTime)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetLastPunchAsync(int employeeId, CancellationToken ct)
    {
        //return await _db.Set<AttendancePunches>()
        //    .Where(ap => ap.EmployeeId == employeeId)
        //    .OrderByDescending(ap => ap.PunchTime)
        //    .FirstOrDefaultAsync(ct);
        var lastPunch = await _db.Set<AttendancePunches>()
            .Where(ap => ap.EmployeeId == employeeId)
            .OrderByDescending(ap => ap.PunchTime)
            .FirstOrDefaultAsync(ct);

        return lastPunch != null ? new List<AttendancePunches> 
            { lastPunch } : Enumerable.Empty<AttendancePunches>();
    }
}
