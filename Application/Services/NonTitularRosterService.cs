using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public sealed class NonTitularRosterService : INonTitularRosterService
{
    private const int MaxPageSize = 100;

    private readonly INonTitularRosterRepository _repo;

    public NonTitularRosterService(INonTitularRosterRepository repo) => _repo = repo;

    public async Task<PagedResult<NonTitularRosterItemDto>> GetPagedAsync(
        NonTitularRosterFilterDto filter, bool canViewSalary, CancellationToken ct)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var rows = await _repo.GetRowsAsync(ToQuery(filter), canViewSalary, page, pageSize, ct);

        return new PagedResult<NonTitularRosterItemDto>
        {
            Items = rows.Select(r => Map(r, canViewSalary)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
        };
    }

    public async Task<IReadOnlyList<NonTitularRosterItemDto>> GetAllAsync(
        NonTitularRosterFilterDto filter, bool canViewSalary, CancellationToken ct)
    {
        var rows = await _repo.GetRowsAsync(ToQuery(filter), canViewSalary, page: null, pageSize: null, ct);
        return rows.Select(r => Map(r, canViewSalary)).ToList();
    }

    public async Task<NonTitularRosterSummaryDto> GetSummaryAsync(NonTitularRosterFilterDto filter, CancellationToken ct)
    {
        var query = ToQuery(filter);

        // Tres conteos en secuencia (BD compartida): todos los filtros para el total, y uno por familia de tarjetas
        // sin su propio filtro. Si ese filtro no está activo el resultado es idéntico y se reutiliza.
        var groups = await _repo.GetGroupsAsync(query, NonTitularRosterIgnore.None, ct);
        var dedicationGroups = filter.DedicationTypeId.HasValue
            ? await _repo.GetGroupsAsync(query, NonTitularRosterIgnore.Dedication, ct)
            : groups;
        var contractTypeGroups = filter.ContractTypeId.HasValue
            ? await _repo.GetGroupsAsync(query, NonTitularRosterIgnore.ContractType, ct)
            : groups;

        var byDedication = dedicationGroups
            .GroupBy(g => (g.DedicationTypeId, g.DedicationName))
            .Select(x => new TeacherRosterCountDto(x.Key.DedicationTypeId, x.Key.DedicationName, null, x.Sum(g => g.Total)))
            .OrderBy(c => c.Name)
            .ToList();

        var byContractType = contractTypeGroups
            .GroupBy(g => (g.ContractTypeId, g.ContractTypeName))
            .Select(x => new TeacherRosterCountDto(x.Key.ContractTypeId, x.Key.ContractTypeName, null, x.Sum(g => g.Total)))
            .OrderBy(c => c.Name)
            .ToList();

        return new NonTitularRosterSummaryDto(
            Total: groups.Sum(g => g.Total),
            VigenteCount: groups.Where(g => g.ContractState == "VIGENTE").Sum(g => g.Total),
            VencidoCount: groups.Where(g => g.ContractState == "VENCIDO").Sum(g => g.Total),
            ByDedication: byDedication,
            ByContractType: byContractType);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Normaliza el filtro de la API: Status = ACTIVE (vigentes, por defecto) | EXPIRED | ALL.</summary>
    private static NonTitularRosterQuery ToQuery(NonTitularRosterFilterDto f) => new(
        Search: string.IsNullOrWhiteSpace(f.Search) ? null : f.Search,
        DedicationTypeId: f.DedicationTypeId,
        ContractTypeId: f.ContractTypeId,
        DepartmentId: f.DepartmentId,
        ContractState: (f.Status?.Trim().ToUpperInvariant()) switch
        {
            "ALL" => null,
            "EXPIRED" => "VENCIDO",
            _ => "VIGENTE",
        });

    private static DateOnly? ToDateOnly(DateTime? d) => d.HasValue ? DateOnly.FromDateTime(d.Value) : null;

    private static NonTitularRosterItemDto Map(NonTitularRosterRow r, bool canViewSalary) => new(
        r.EmployeeId,
        r.FullName ?? string.Empty,
        r.IdCard,
        r.EmployeeIsActive,
        r.DepartmentId,
        r.DepartmentName,
        r.FacultyName,
        r.JobName,
        r.DedicationTypeId,
        r.DedicationName,
        r.ContractedHours,
        r.ContractTypeId,
        r.ContractTypeName,
        r.ContractCode,
        DateOnly.FromDateTime(r.ContractStartDate),
        ToDateOnly(r.ContractEndDate),
        r.ContractState,
        // Defensa en profundidad: aunque el SQL ya no consulta el sueldo sin permiso, se vuelve a anular aquí.
        canViewSalary ? r.Rmu : null,
        canViewSalary ? r.RmuSource : null,
        canViewSalary ? r.RmuInForce : null,
        canViewSalary ? r.RmuNeedsReview == 1 : null);
}
