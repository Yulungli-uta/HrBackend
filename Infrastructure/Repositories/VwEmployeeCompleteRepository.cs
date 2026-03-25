using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Employees;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class VwEmployeeCompleteRepository : IvwEmployeeCompleteRepository
    {
        private readonly AppDbContext _context;

        public VwEmployeeCompleteRepository(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<VwEmployeeComplete> Query() =>
            _context.vwEmployeeComplete
                .AsNoTracking();

        public async Task<IEnumerable<VwEmployeeComplete>> GetAllAsync(CancellationToken ct = default)
        {
            return await Query()
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync(ct);
        }

        public async Task<VwEmployeeComplete?> GetByIdAsync(int employeeId, CancellationToken ct = default)
        {
            return await Query()
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId, ct);
        }

        public async Task<IEnumerable<VwEmployeeComplete>> GetByDepartmentAsync(
            string department,
            CancellationToken ct = default)
        {
            return await Query()
                .Where(e => e.Department == department)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync(ct);
        }

        public async Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = Query()
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName);

            var totalCount = await query.LongCountAsync(ct);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<VwEmployeeComplete>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = Query();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                query = query.Where(e =>
                    (e.FirstName != null && e.FirstName.ToLower().Contains(term)) ||
                    (e.LastName != null && e.LastName.ToLower().Contains(term)) ||
                    (e.FullName != null && e.FullName.ToLower().Contains(term)) ||
                    (e.IDCard != null && e.IDCard.ToLower().Contains(term)) ||
                    (e.Email != null && e.Email.ToLower().Contains(term)));
            }

            var totalCount = await query.LongCountAsync(ct);

            var items = await query
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<VwEmployeeComplete>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        public async Task<List<ContractTypeCountDto>> GetByContractTypeAsync(CancellationToken ct = default)
        {
            return await Query()
                .Where(e =>
                    e.EmployeeIsActive &&
                    e.EmployeeType != null &&
                    e.EmployeeType != 0)
                .GroupBy(e => e.EmployeeType)
                .Select(g => new ContractTypeCountDto
                {
                    EmployeeType = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.EmployeeType)
                .ToListAsync(ct);
        }

        public async Task<EmployeeCompleteStatsDto> GetStatsAsync(CancellationToken ct = default)
        {
            var total = await Query().LongCountAsync(ct);

            var active = await Query()
                .LongCountAsync(e => e.EmployeeIsActive, ct);

            var inactive = total - active;

            var byContractType = await GetByContractTypeAsync(ct);

            return new EmployeeCompleteStatsDto
            {
                Total = total,
                Active = active,
                Inactive = inactive,
                ByContractType = byContractType
            };
        }
    }
}