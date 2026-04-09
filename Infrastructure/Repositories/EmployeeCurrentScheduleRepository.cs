using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class EmployeeCurrentScheduleRepository : IEmployeeCurrentScheduleRepository
    {
        private readonly AppDbContext _context;

        public EmployeeCurrentScheduleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VwEmployeeCurrentSchedule?> GetByEmployeeIdAsync(
            int employeeId,
            CancellationToken cancellationToken = default)
        {
            return await _context.VwEmployeeCurrentSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId, cancellationToken);
        }

        public async Task<IReadOnlyList<VwEmployeeCurrentSchedule>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.VwEmployeeCurrentSchedules
                .AsNoTracking()
                .OrderBy(x => x.EmployeeId)
                .ToListAsync(cancellationToken);
        }
        public async Task<IReadOnlyList<VwEmployeeCurrentSchedule>> GetByEmployeeIdsAsync(
        IEnumerable<int> employeeIds,
        CancellationToken cancellationToken = default)
        {
            var ids = employeeIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return Array.Empty<VwEmployeeCurrentSchedule>();

            return await _context.VwEmployeeCurrentSchedules
                .AsNoTracking()
                .Where(x => ids.Contains(x.EmployeeId))
                .ToListAsync(cancellationToken);
        }
    }
}
