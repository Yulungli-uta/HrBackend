using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public sealed class TeacherRosterService : ITeacherRosterService
{
    private const int MaxPageSize = 100;

    private readonly ITeacherRosterRepository _repo;

    public TeacherRosterService(ITeacherRosterRepository repo) => _repo = repo;

    public async Task<PagedResult<TeacherRosterItemDto>> GetPagedAsync(
        TeacherRosterFilterDto filter, bool canViewSalary, CancellationToken ct)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var rows = await _repo.GetRowsAsync(ToQuery(filter), canViewSalary, page, pageSize, ct);

        return new PagedResult<TeacherRosterItemDto>
        {
            Items = rows.Select(r => Map(r, canViewSalary)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
        };
    }

    public async Task<IReadOnlyList<TeacherRosterItemDto>> GetAllAsync(
        TeacherRosterFilterDto filter, bool canViewSalary, CancellationToken ct)
    {
        var rows = await _repo.GetRowsAsync(ToQuery(filter), canViewSalary, page: null, pageSize: null, ct);
        return rows.Select(r => Map(r, canViewSalary)).ToList();
    }

    public async Task<TeacherRosterSummaryDto> GetSummaryAsync(TeacherRosterFilterDto filter, CancellationToken ct)
    {
        var query = ToQuery(filter);

        // Tres conteos, uno por familia de tarjetas, en secuencia (la BD es compartida: sin paralelismo):
        //  - groups: todos los filtros → total, activos/inactivos, alertas y matriz.
        //  - categoryGroups: sin categoría/nivel → tarjetas de categoría y sus niveles.
        //  - dedicationGroups: sin dedicación → tarjetas de dedicación.
        var groups = await _repo.GetGroupsAsync(query, TeacherRosterIgnore.None, ct);
        var categoryGroups = filter.CategoryTypeId.HasValue || filter.LevelTypeId.HasValue
            ? await _repo.GetGroupsAsync(query, TeacherRosterIgnore.CategoryAndLevel, ct)
            : groups;
        var dedicationGroups = filter.DedicationTypeId.HasValue
            ? await _repo.GetGroupsAsync(query, TeacherRosterIgnore.Dedication, ct)
            : groups;
        var withoutStructure = await _repo.CountActiveWithScaleActionWithoutStructureAsync(ct);

        var byCategory = categoryGroups
            .GroupBy(g => (g.CategoryTypeId, g.CategoryName))
            .Select(x => new TeacherRosterCountDto(x.Key.CategoryTypeId, x.Key.CategoryName, null, x.Sum(g => g.Total)))
            .OrderBy(c => c.Name)
            .ToList();

        var byCategoryLevel = categoryGroups
            .GroupBy(g => (g.CategoryTypeId, g.LevelTypeId, g.LevelName))
            .Select(x => new TeacherRosterCountDto(x.Key.LevelTypeId, x.Key.LevelName, x.Key.CategoryTypeId, x.Sum(g => g.Total)))
            .OrderBy(c => c.ParentTypeId).ThenBy(c => c.Name)
            .ToList();

        var byDedication = dedicationGroups
            .GroupBy(g => (g.DedicationTypeId, g.DedicationName))
            .Select(x => new TeacherRosterCountDto(x.Key.DedicationTypeId, x.Key.DedicationName, null, x.Sum(g => g.Total)))
            .OrderBy(c => c.Name)
            .ToList();

        var matrix = groups
            .GroupBy(g => (g.CategoryTypeId, g.CategoryName, g.DedicationTypeId, g.DedicationName))
            .Select(x => new TeacherRosterMatrixCellDto(
                x.Key.CategoryTypeId, x.Key.CategoryName, x.Key.DedicationTypeId, x.Key.DedicationName, x.Sum(g => g.Total)))
            .ToList();

        return new TeacherRosterSummaryDto(
            Total: groups.Sum(g => g.Total),
            ActiveCount: groups.Where(g => g.EmployeeIsActive).Sum(g => g.Total),
            InactiveCount: groups.Where(g => !g.EmployeeIsActive).Sum(g => g.Total),
            ByCategory: byCategory,
            ByCategoryLevel: byCategoryLevel,
            ByDedication: byDedication,
            CategoryByDedication: matrix,
            PendingScaleActionCount: groups.Sum(g => g.PendingScale),
            LadderMismatchCount: groups.Sum(g => g.Mismatch),
            ActiveWithScaleActionWithoutStructure: withoutStructure);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Normaliza el filtro de la API: Status = ACTIVE (por defecto) | INACTIVE | ALL.</summary>
    private static TeacherRosterQuery ToQuery(TeacherRosterFilterDto f) => new(
        Search: string.IsNullOrWhiteSpace(f.Search) ? null : f.Search,
        CategoryTypeId: f.CategoryTypeId,
        LevelTypeId: f.LevelTypeId,
        DedicationTypeId: f.DedicationTypeId,
        DepartmentId: f.DepartmentId,
        EmployeeIsActive: (f.Status?.Trim().ToUpperInvariant()) switch
        {
            "ALL" => null,
            "INACTIVE" => false,
            _ => true,
        },
        PendingScaleAction: f.PendingScaleAction,
        LadderMismatch: f.LadderMismatch);

    private static DateOnly? ToDateOnly(DateTime? d) => d.HasValue ? DateOnly.FromDateTime(d.Value) : null;

    private static TeacherRosterItemDto Map(TeacherRosterRow r, bool canViewSalary) => new(
        r.TeacherStructureId,
        r.EmployeeId,
        r.FullName ?? string.Empty,
        r.IdCard,
        r.EmployeeIsActive,
        r.DepartmentId,
        r.DepartmentName,
        r.FacultyName,
        r.LadderId,
        r.LadderName,
        r.CategoryTypeId,
        r.CategoryName,
        r.LevelTypeId,
        r.LevelName,
        r.DedicationTypeId,
        r.DedicationName,
        r.WeeklyClassHours,
        DateOnly.FromDateTime(r.StructureStartDate),
        ToDateOnly(r.AppointmentDate),
        r.AppointmentKind,
        r.AppointmentActionNumber,
        // Defensa en profundidad: aunque el SQL ya no consulta el sueldo sin permiso, se vuelve a anular aquí.
        canViewSalary ? r.Rmu : null,
        canViewSalary ? r.RmuSource : null,
        canViewSalary ? r.RmuInForce : null,
        canViewSalary ? r.RmuNeedsReview == 1 : null,
        r.PendingScaleActionNumber,
        r.PendingScaleActionType,
        ToDateOnly(r.PendingScaleActionDate),
        r.LadderMismatchFlag == 1);
}
