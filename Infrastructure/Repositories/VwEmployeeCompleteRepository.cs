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
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
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
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync(ct);
        }

        public async Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = Query()
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName);

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

            // Búsqueda por palabra: cada palabra escrita (ej. "Perez Juan") debe
            // aparecer en ALGUNO de los campos, no las dos juntas en un solo campo.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var words = search.Trim().ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var w = word;
                    query = query.Where(e =>
                        (e.FirstName != null && e.FirstName.ToLower().Contains(w)) ||
                        (e.LastName != null && e.LastName.ToLower().Contains(w)) ||
                        (e.FullName != null && e.FullName.ToLower().Contains(w)) ||
                        (e.IDCard != null && e.IDCard.ToLower().Contains(w)) ||
                        (e.Email != null && e.Email.ToLower().Contains(w)) ||
                        (e.Department != null && e.Department.ToLower().Contains(w)));
                }
            }

            var totalCount = await query.LongCountAsync(ct);

            var items = await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
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
            int? employeeType,
            string? department,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = Query();

            // Búsqueda por palabra: cada palabra escrita (ej. "Perez Juan") debe
            // aparecer en ALGUNO de los campos, no las dos juntas en un solo campo.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var words = search.Trim().ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var w = word;
                    query = query.Where(e =>
                        (e.FirstName != null && e.FirstName.ToLower().Contains(w)) ||
                        (e.LastName != null && e.LastName.ToLower().Contains(w)) ||
                        (e.FullName != null && e.FullName.ToLower().Contains(w)) ||
                        (e.IDCard != null && e.IDCard.ToLower().Contains(w)) ||
                        (e.Email != null && e.Email.ToLower().Contains(w)) ||
                        (e.Department != null && e.Department.ToLower().Contains(w)));
                }
            }

            if (employeeType.HasValue)
            {
                query = query.Where(e => e.EmployeeType == employeeType.Value);
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(e => e.Department == department);
            }

            if (isActive.HasValue)
            {
                query = query.Where(e => e.EmployeeIsActive == isActive.Value);
            }

            var totalCount = await query.LongCountAsync(ct);

            var items = await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
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
                .GroupBy(e => e.EmployeeType!.Value)
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