using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
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
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByDepartmentAsync(
            string departmentName,
            CancellationToken ct = default)
        {
            return await Query()
                .Where(e => e.Department == departmentName)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByImmediateBossIdAsync(
            int bossId,
            CancellationToken ct = default)
        {
            return await Query()
                .Where(e => e.ImmediateBossID == bossId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
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
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByEmployeeTypeAsync(
            int employeeType,
            CancellationToken ct = default)
        {
            return await Query()
                .Where(e => e.EmployeeType == employeeType)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<int>> GetEmployeeTypesAsync(CancellationToken ct = default)
        {
            return await Query()
                .Where(e => e.EmployeeType.HasValue)
                .Select(e => e.EmployeeType!.Value)
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
                .FirstOrDefaultAsync(e =>
                    e.Email == normalizedEmail ||
                    e.PersonnelEmail == normalizedEmail,
                    ct);
        }

        public async Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
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
            // Búsqueda por palabra: cada palabra escrita (ej. "Perez Juan") debe
            // aparecer en ALGUNO de los campos, no las dos juntas en un solo campo —
            // antes comparaba la frase completa contra cada campo por separado, lo
            // que nunca coincidía si el usuario escribía apellido y nombre juntos.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var words = search.Trim().ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var w = word;
                    query = query.Where(e =>
                        e.FirstName.ToLower().Contains(w) ||
                        e.LastName.ToLower().Contains(w) ||
                        e.IDCard.ToLower().Contains(w) ||
                        (e.Email != null && e.Email.ToLower().Contains(w)));
                }
            }

            // 3. Contamos antes de ordenar (es más eficiente)
            var totalCount = await query.LongCountAsync(ct);

            // 4. Ordenamos y paginamos al final
            var items = await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
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

        public async Task<IEnumerable<VwEmployeeDetails>> GetByFiltersAsync(
    int? departmentId,
    int? employeeType,
    int? laborRegimeId = null,
    CancellationToken ct = default)
        {
            var query = Query();

            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(e => e.DepartmentID == departmentId.Value);

            if (employeeType.HasValue && employeeType.Value > 0)
                query = query.Where(e => e.EmployeeType == employeeType.Value);

            // Régimen laboral: prioriza EmployeeLaborRegime; si el empleado no tiene ningún
            // registro activo ahí (~34% de los activos, ver análisis 2026-08-11), cae a
            // EmployeeType legacy en vez de excluirlo en silencio del reporte.
            if (laborRegimeId.HasValue && laborRegimeId.Value > 0)
            {
                var regimeId = laborRegimeId.Value;
                query = query.Where(e =>
                    _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeID && r.IsActive && r.LaborRegimeId == regimeId)
                    || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeID && r.IsActive)
                        && e.EmployeeType == regimeId));
            }

            return await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<DepartmentContractCountDto>> GetDepartmentContractCountsAsync(
            int? departmentId,
            int? employeeType,
            int? laborRegimeId = null,
            CancellationToken ct = default)
        {
            var query = Query();

            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(e => e.DepartmentID == departmentId.Value);

            if (employeeType.HasValue && employeeType.Value > 0)
                query = query.Where(e => e.EmployeeType == employeeType.Value);

            // Régimen laboral: prioriza EmployeeLaborRegime; si el empleado no tiene ningún
            // registro activo ahí, cae a EmployeeType legacy (ver GetByFiltersAsync de esta
            // misma clase para la justificación completa).
            if (laborRegimeId.HasValue && laborRegimeId.Value > 0)
            {
                var regimeId = laborRegimeId.Value;
                query = query.Where(e =>
                    _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeID && r.IsActive && r.LaborRegimeId == regimeId)
                    || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeID && r.IsActive)
                        && e.EmployeeType == regimeId));
            }

            return await query
                .GroupBy(e => new
                {
                    e.DepartmentID,
                    e.Department,
                    e.ContractType
                })
                .Select(g => new DepartmentContractCountDto
                {
                    DepartmentID = g.Key.DepartmentID,
                    Department = string.IsNullOrWhiteSpace(g.Key.Department)
                        ? "Sin dependencia"
                        : g.Key.Department,
                    ContractType = string.IsNullOrWhiteSpace(g.Key.ContractType)
                        ? "Sin contrato"
                        : g.Key.ContractType,
                    TotalEmployees = g.Count()
                })
                .OrderBy(x => x.Department)
                .ThenBy(x => x.ContractType)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ScheduleContractCountDto>> GetScheduleContractCountsAsync(
            int? departmentId,
            int? employeeType,
            int? laborRegimeId = null,
            int? departmentTypeId = null,
            int? departmentScopeId = null,
            CancellationToken ct = default)
        {
            var query = Query();

            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(e => e.DepartmentID == departmentId.Value);

            if (employeeType.HasValue && employeeType.Value > 0)
                query = query.Where(e => e.EmployeeType == employeeType.Value);

            // Régimen laboral: prioriza EmployeeLaborRegime; si el empleado no tiene ningún
            // registro activo ahí, cae a EmployeeType legacy (ver GetByFiltersAsync de esta
            // misma clase para la justificación completa).
            if (laborRegimeId.HasValue && laborRegimeId.Value > 0)
            {
                var regimeId = laborRegimeId.Value;
                query = query.Where(e =>
                    _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeID && r.IsActive && r.LaborRegimeId == regimeId)
                    || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeID && r.IsActive)
                        && e.EmployeeType == regimeId));
            }

            // Tipo/Ámbito de dependencia: vw_EmployeeDetails no trae estas columnas, así
            // que se resuelven primero los DepartmentID elegibles contra tbl_Departments.
            if ((departmentTypeId.HasValue && departmentTypeId.Value > 0) || (departmentScopeId.HasValue && departmentScopeId.Value > 0))
            {
                var eligibleDeptIds = _db.Set<Departments>().AsNoTracking()
                    .Where(d => (!departmentTypeId.HasValue || departmentTypeId.Value <= 0 || d.DepartmentType == departmentTypeId.Value)
                             && (!departmentScopeId.HasValue || departmentScopeId.Value <= 0 || d.DepartmentScope == departmentScopeId.Value))
                    .Select(d => d.DepartmentId);
                query = query.Where(e => e.DepartmentID.HasValue && eligibleDeptIds.Contains(e.DepartmentID.Value));
            }

            var grouped = await query
                .GroupBy(e => new
                {
                    e.DepartmentID,
                    e.Department,
                    e.ScheduleID,
                    e.Schedule,
                    e.ContractType
                })
                .Select(g => new ScheduleContractCountDto
                {
                    DepartmentID = g.Key.DepartmentID,
                    DepartmentName = string.IsNullOrWhiteSpace(g.Key.Department)
                        ? "Sin dependencia"
                        : g.Key.Department,
                    ScheduleID = g.Key.ScheduleID,
                    Schedule = string.IsNullOrWhiteSpace(g.Key.Schedule)
                        ? "Sin horario"
                        : g.Key.Schedule,
                    ContractType = string.IsNullOrWhiteSpace(g.Key.ContractType)
                        ? "Sin contrato"
                        : g.Key.ContractType,
                    TotalEmployees = g.Count()
                })
                .OrderBy(x => x.DepartmentName)
                .ThenBy(x => x.Schedule)
                .ThenBy(x => x.ContractType)
                .ToListAsync(ct);

            var deptIds = grouped.Where(x => x.DepartmentID.HasValue).Select(x => x.DepartmentID!.Value).Distinct().ToList();
            var deptInfo = await _db.Set<Departments>().AsNoTracking()
                .Where(d => deptIds.Contains(d.DepartmentId))
                .Select(d => new { d.DepartmentId, d.DepartmentType, d.DepartmentScope })
                .ToListAsync(ct);
            var refTypeIds = deptInfo.SelectMany(d => new[] { d.DepartmentType, d.DepartmentScope })
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var refTypeNames = await _db.Set<RefTypes>().AsNoTracking()
                .Where(r => refTypeIds.Contains(r.TypeId))
                .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);
            var deptInfoById = deptInfo.ToDictionary(d => d.DepartmentId);

            foreach (var row in grouped)
            {
                if (!row.DepartmentID.HasValue || !deptInfoById.TryGetValue(row.DepartmentID.Value, out var info)) continue;
                row.DepartmentTypeName = info.DepartmentType.HasValue && refTypeNames.TryGetValue(info.DepartmentType.Value, out var tn) ? tn : null;
                row.DepartmentScopeName = info.DepartmentScope.HasValue && refTypeNames.TryGetValue(info.DepartmentScope.Value, out var sn) ? sn : null;
            }

            return grouped;
        }

        public async Task<ScheduleCoverageStatsDto> GetScheduleCoverageStatsAsync(CancellationToken ct = default)
        {
            var total = await Query().CountAsync(ct);
            var withSchedule = await Query()
                .CountAsync(e => e.ScheduleID != null || !string.IsNullOrEmpty(e.Schedule), ct);

            return new ScheduleCoverageStatsDto
            {
                Total = total,
                WithSchedule = withSchedule,
                WithoutSchedule = total - withSchedule
            };
        }
    }
}
