using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class VwEmployeeDetailsRepository : ServiceAwareEfRepository<VwEmployeeDetails, int>, IvwEmployeeDetailsRepository
    {
        private readonly DbContext _db;

        public VwEmployeeDetailsRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<VwEmployeeDetails?> GetByIdAsync(int employeeId, CancellationToken ct = default)
        {
            var result = await _db.Set<VwEmployeeDetails>()
                .Where(e => e.EmployeeID == employeeId)
                .Select(e => new VwEmployeeDetails
                {
                    EmployeeID = e.EmployeeID,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    //FullName = e.FirstName + " " + e.LastName,
                    IDCard = e.IDCard,
                    Email = e.Email,
                    EmployeeType = e.EmployeeType,
                    Department = e.Department,
                   // Faculty = e.Faculty,
                    BaseSalary = e.BaseSalary,
                    HireDate = e.HireDate,
                    //HasActiveSalary = e.BaseSalary.HasValue && e.BaseSalary > 0
                })
                .FirstOrDefaultAsync(ct);

            return result;
        }

        //public async Task<PagedResultDto<EmployeeDetailsDto>> GetPagedAsync(
        //    EmployeeDetailsFilterDto filter,
        //    CancellationToken ct = default)
        //{
        //    var query = _db.Set<EmployeeDetails>().AsQueryable();

        //    // Apply filters
        //    query = ApplyFilters(query, filter);

        //    // Get total count
        //    var totalCount = await query.CountAsync(ct);

        //    // Apply sorting
        //    query = ApplySorting(query, filter.SortBy, filter.SortDescending);

        //    // Apply paging
        //    var items = await query
        //        .Skip((filter.PageNumber - 1) * filter.PageSize)
        //        .Take(filter.PageSize)
        //        .Select(e => new EmployeeDetailsDto
        //        {
        //            EmployeeID = e.EmployeeID,
        //            FirstName = e.FirstName,
        //            LastName = e.LastName,
        //            FullName = e.FirstName + " " + e.LastName,
        //            IDCard = e.IDCard,
        //            Email = e.Email,
        //            EmployeeType = e.EmployeeType,
        //            Department = e.Department,
        //            Faculty = e.Faculty,
        //            BaseSalary = e.BaseSalary,
        //            HireDate = e.HireDate,
        //            HasActiveSalary = e.BaseSalary.HasValue && e.BaseSalary > 0
        //        })
        //        .ToListAsync(ct);

        //    return new PagedResultDto<EmployeeDetailsDto>
        //    {
        //        Items = items,
        //        TotalCount = totalCount,
        //        PageNumber = filter.PageNumber,
        //        PageSize = filter.PageSize
        //    };
        //}

        public async Task<IEnumerable<VwEmployeeDetails>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new VwEmployeeDetails
                {
                    EmployeeID = e.EmployeeID,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    //FullName = e.FirstName + " " + e.LastName,
                    IDCard = e.IDCard,
                    Email = e.Email,
                    EmployeeType = e.EmployeeType,
                    Department = e.Department,
                    //Faculty = e.Faculty,
                    BaseSalary = e.BaseSalary,
                    HireDate = e.HireDate
                    //HasActiveSalary = e.BaseSalary.HasValue && e.BaseSalary > 0
                })
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByDepartmentAsync(
            string departmentName,
            CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                .Where(e => e.Department == departmentName)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new VwEmployeeDetails
                {
                    EmployeeID = e.EmployeeID,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    //FullName = e.FirstName + " " + e.LastName,
                    IDCard = e.IDCard,
                    Email = e.Email,
                    EmployeeType = e.EmployeeType,
                    Department = e.Department,
                    //Faculty = e.Faculty,
                    BaseSalary = e.BaseSalary,
                    HireDate = e.HireDate
                    //HasActiveSalary = e.BaseSalary.HasValue && e.BaseSalary > 0
                })
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByFacultyAsync(
            string facultyName,
            CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                .Where(e => e.Department == facultyName)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new VwEmployeeDetails
                {
                    EmployeeID = e.EmployeeID,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    //FullName = e.FirstName + " " + e.LastName,
                    IDCard = e.IDCard,
                    Email = e.Email,
                    EmployeeType = e.EmployeeType,
                    Department = e.Department,
                    //Faculty = e.Faculty,
                    BaseSalary = e.BaseSalary,
                    HireDate = e.HireDate
                    //HasActiveSalary = e.BaseSalary.HasValue && e.BaseSalary > 0
                })
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetByEmployeeTypeAsync(
            int employeeType,
            CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                .Where(e => e.EmployeeType == employeeType)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new VwEmployeeDetails
                {
                    EmployeeID = e.EmployeeID,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    //FullName = e.FirstName + " " + e.LastName,
                    IDCard = e.IDCard,
                    Email = e.Email,
                    EmployeeType = e.EmployeeType,
                    Department = e.Department,
                    //Faculty = e.Faculty,
                    BaseSalary = e.BaseSalary,
                    HireDate = e.HireDate
                    //HasActiveSalary = e.BaseSalary.HasValue && e.BaseSalary > 0
                })
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<int>> GetEmployeeTypesAsync(CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                .Select(e => e.EmployeeType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<string>> GetDepartmentsAsync(CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                .Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<string>> GetFacultiesAsync(CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                .Where(e => !string.IsNullOrEmpty(e.Department))
                .Select(e => e.Department!)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync(ct);
        }

        public async Task<VwEmployeeDetails?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _db.Set<VwEmployeeDetails>()
                  .AsNoTracking()
                  .FirstOrDefaultAsync (e => e.Email == email);
        }

        //private static IQueryable<VwEmployeeDetails> ApplyFilters(
        //    IQueryable<VwEmployeeDetails> query,
        //    EmployeeDetailsFilterDto filter)
        //{
        //    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        //    {
        //        var searchTerm = filter.SearchTerm.ToLower();
        //        query = query.Where(e =>
        //            e.FirstName.ToLower().Contains(searchTerm) ||
        //            e.LastName.ToLower().Contains(searchTerm) ||
        //            e.IDCard.ToLower().Contains(searchTerm) ||
        //            e.Email.ToLower().Contains(searchTerm));
        //    }

        //    if (!string.IsNullOrWhiteSpace(filter.EmployeeType))
        //    {
        //        query = query.Where(e => e.EmployeeType == filter.EmployeeType);
        //    }

        //    if (!string.IsNullOrWhiteSpace(filter.Department))
        //    {
        //        query = query.Where(e => e.Department == filter.Department);
        //    }

        //    if (!string.IsNullOrWhiteSpace(filter.Faculty))
        //    {
        //        query = query.Where(e => e.Faculty == filter.Faculty);
        //    }

        //    if (filter.MinSalary.HasValue)
        //    {
        //        query = query.Where(e => e.BaseSalary >= filter.MinSalary.Value);
        //    }

        //    if (filter.MaxSalary.HasValue)
        //    {
        //        query = query.Where(e => e.BaseSalary <= filter.MaxSalary.Value);
        //    }

        //    if (filter.HireDateFrom.HasValue)
        //    {
        //        query = query.Where(e => e.HireDate >= filter.HireDateFrom.Value);
        //    }

        //    if (filter.HireDateTo.HasValue)
        //    {
        //        query = query.Where(e => e.HireDate <= filter.HireDateTo.Value);
        //    }

        //    return query;
        //}

        //private static IQueryable<VwEmployeeDetails> ApplySorting(
        //    IQueryable<VwEmployeeDetails> query,
        //    string? sortBy,
        //    bool sortDescending)
        //{
        //    return (sortBy?.ToLower()) switch
        //    {
        //        "firstname" => sortDescending
        //            ? query.OrderByDescending(e => e.FirstName)
        //            : query.OrderBy(e => e.FirstName),

        //        "lastname" => sortDescending
        //            ? query.OrderByDescending(e => e.LastName)
        //            : query.OrderBy(e => e.LastName),

        //        "idcard" => sortDescending
        //            ? query.OrderByDescending(e => e.IDCard)
        //            : query.OrderBy(e => e.IDCard),

        //        "email" => sortDescending
        //            ? query.OrderByDescending(e => e.Email)
        //            : query.OrderBy(e => e.Email),

        //        "employeetype" => sortDescending
        //            ? query.OrderByDescending(e => e.EmployeeType)
        //            : query.OrderBy(e => e.EmployeeType),

        //        "department" => sortDescending
        //            ? query.OrderByDescending(e => e.Department)
        //            : query.OrderBy(e => e.Department),

        //        "faculty" => sortDescending
        //            ? query.OrderByDescending(e => e.Faculty)
        //            : query.OrderBy(e => e.Faculty),

        //        "basesalary" => sortDescending
        //            ? query.OrderByDescending(e => e.BaseSalary)
        //            : query.OrderBy(e => e.BaseSalary),

        //        "hiredate" => sortDescending
        //            ? query.OrderByDescending(e => e.HireDate)
        //            : query.OrderBy(e => e.HireDate),

        //        _ => sortDescending
        //            ? query.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName)
        //            : query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
        //    };
        //}
    }

}
