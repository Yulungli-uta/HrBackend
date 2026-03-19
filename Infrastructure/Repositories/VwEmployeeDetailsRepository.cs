using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class VwEmployeeDetailsRepository
        : ServiceAwareEfRepository<VwEmployeeDetails, int>, IvwEmployeeDetailsRepository
    {
        private readonly DbContext _db;

        public VwEmployeeDetailsRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        private IQueryable<VwEmployeeDetails> Query() =>
            _db.Set<VwEmployeeDetails>()
               .AsNoTracking();

        public async Task<VwEmployeeDetails?> GetByIdAsync(int employeeId, CancellationToken ct = default)
        {
            return await Query()
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId, ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetAllAsync(CancellationToken ct = default)
        {
            return await Query()
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByDepartmentAsync(
            string departmentName,
            CancellationToken ct = default)
        {
            return await Query()
                .Where(e => e.Department == departmentName)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByFacultyAsync(
            string facultyName,
            CancellationToken ct = default)
        {
            // Nota: tu lógica actual filtra por Department == facultyName
            // Si Faculty existe en la vista, debería usarse esa columna.
            return await Query()
                .Where(e => e.Department == facultyName)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByEmployeeTypeAsync(
            int employeeType,
            CancellationToken ct = default)
        {
            return await Query()
                .Where(e => e.EmployeeType == employeeType)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<int>> GetEmployeeTypesAsync(CancellationToken ct = default)
        {
            return await Query()
                .Select(e => e.EmployeeType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<string>> GetDepartmentsAsync(CancellationToken ct = default)
        {
            return await Query()
                .Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<string>> GetFacultiesAsync(CancellationToken ct = default)
        {
            // Nota: actualmente es equivalente a GetDepartmentsAsync.
            // Si Faculty existe en la vista, cámbialo a esa columna.
            return await Query()
                .Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department!)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync(ct);
        }

        public async Task<VwEmployeeDetails?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalizedEmail = (email ?? "").Trim();

            return await Query()
                .FirstOrDefaultAsync(e => e.Email == normalizedEmail, ct);
        }

        public async Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
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

            return new PagedResult<VwEmployeeDetails>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Retorna empleados paginados con búsqueda por nombre, apellido, cédula o email.
        /// Si search es null o vacío, retorna todos sin filtro.
        /// </summary>
        public async Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            // 1. Empezamos con la consulta base
            var query = Query();

            // 2. Aplicamos filtros (mantiene el tipo IQueryable)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    e.FirstName.ToLower().Contains(term) ||
                    e.LastName.ToLower().Contains(term) ||
                    e.IDCard.ToLower().Contains(term) ||
                    e.Email.ToLower().Contains(term));
            }

            // 3. Contamos antes de ordenar (es más eficiente)
            var totalCount = await query.LongCountAsync(ct);

            // 4. Ordenamos y paginamos al final
            var items = await query
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<VwEmployeeDetails>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
