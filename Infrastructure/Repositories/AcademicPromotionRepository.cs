using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class AcademicPromotionRepository : IAcademicPromotionRepository
{
    private const string FacultyDepartmentTypeName = "FACULTAD";
    private const int MaxHierarchyDepth = 10; // salvaguarda ante ciclos accidentales en ParentID

    private readonly AppDbContext _db;

    public AcademicPromotionRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<AcademicPromotionEmployeeLookup?> FindEmployeeByIdentificationAsync(string identification, CancellationToken ct = default)
    {
        var result = await (
            from person in _db.People.AsNoTracking()
            where person.IdCard == identification
            join employee in _db.Employees.AsNoTracking() on person.PersonId equals employee.PersonID
            select new AcademicPromotionEmployeeLookup(
                person.PersonId,
                employee.EmployeeId,
                person.IdCard,
                person.LastName + " " + person.FirstName,
                employee.DepartmentId)
        ).FirstOrDefaultAsync(ct);

        return result;
    }

    public async Task<AcademicPromotionDependency?> FindFacultyDependencyAsync(int? departmentId, CancellationToken ct = default)
    {
        if (departmentId is null) return null;

        var facultyTypeId = await _db.RefTypes.AsNoTracking()
            .Where(rt => rt.Category == "DEPARTMENT_TYPE" && rt.Name == FacultyDepartmentTypeName)
            .Select(rt => (int?)rt.TypeId)
            .FirstOrDefaultAsync(ct);

        var currentId = departmentId;
        for (var depth = 0; depth < MaxHierarchyDepth && currentId is not null; depth++)
        {
            var dept = await _db.Departments.AsNoTracking()
                .Where(d => d.DepartmentId == currentId.Value)
                .Select(d => new { d.DepartmentId, d.Name, d.DepartmentType, d.ParentId })
                .FirstOrDefaultAsync(ct);

            if (dept is null) return null;

            if (facultyTypeId is not null && dept.DepartmentType == facultyTypeId)
                return new AcademicPromotionDependency(dept.DepartmentId, dept.Name);

            currentId = dept.ParentId;
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetRefTypeNamesAsync(IEnumerable<int?> typeIds, CancellationToken ct = default)
    {
        var ids = typeIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();

        var rows = await _db.RefTypes.AsNoTracking()
            .Where(rt => ids.Contains(rt.TypeId))
            .Select(rt => new { rt.TypeId, rt.Name })
            .ToListAsync(ct);

        // .Trim(): algunos valores de ref_Types tienen espacios al final cargados manualmente
        // (ej. "CURSO EN EL CAMPO DE DOCENCIA UNIVERSITARIA "), y SQL Server los considera
        // iguales por padding pero C# no — sin el Trim, comparaciones exactas como el
        // switch de categoría pedagógica/disciplinar en AcademicPromotionService fallan en silencio.
        return rows.ToDictionary(r => r.TypeId, r => r.Name.Trim());
    }

    public async Task<bool> UserHasAnyRoleAsync(string? userId, IReadOnlyCollection<string> roleNames, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        return await _db.VwUserRoles.AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && roleNames.Contains(ur.RoleName), ct);
    }
}
