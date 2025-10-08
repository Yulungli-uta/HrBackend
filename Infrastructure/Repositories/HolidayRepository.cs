using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class HolidayRepository : ServiceAwareEfRepository<Holiday, int>, IHolidayRepository
    {
        private readonly DbContext _db;

        public HolidayRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken ct = default)
        {
            return await _db.Set<Holiday>()
                .Where(h => h.HolidayDate.Year == year && h.IsActive)
                .OrderBy(h => h.HolidayDate)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Holiday>> GetActiveHolidaysAsync(CancellationToken ct = default)
        {
            return await _db.Set<Holiday>()
                .Where(h => h.IsActive)
                .OrderBy(h => h.HolidayDate)
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsByDateAsync(DateTime holidayDate, int? excludeHolidayId = null, CancellationToken ct = default)
        {
            var query = _db.Set<Holiday>().Where(h => h.HolidayDate == holidayDate && h.IsActive);

            if (excludeHolidayId.HasValue)
                query = query.Where(h => h.HolidayID != excludeHolidayId.Value);

            return await query.AnyAsync(ct);
        }

        public async Task<bool> IsHolidayAsync(DateTime date, CancellationToken ct = default)
        {
            return await _db.Set<Holiday>()
                .AnyAsync(h => h.HolidayDate == date && h.IsActive, ct);
        }

        public async Task<IEnumerable<DateTime>> GetHolidayDatesBetweenAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _db.Set<Holiday>()
                .Where(h => h.HolidayDate >= startDate && h.HolidayDate <= endDate && h.IsActive)
                .Select(h => h.HolidayDate)
                .ToListAsync(ct);
        }
    }
}
