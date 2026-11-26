using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.PunchPolicy;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class PunchPolicyRepository : IPunchPolicyRepository
{
    private const string ModeParameterName = "PUNCH_NETWORK_MODE";
    private readonly AppDbContext _db;

    private readonly IAppClock _clock;

    public PunchPolicyRepository(AppDbContext db, IAppClock clock)
    {
        _db = db;
        _clock = clock;
    }

    // ── Redes del campus ────────────────────────────────────────────────────

    public Task<List<AllowedPunchNetwork>> GetNetworksAsync(CancellationToken ct) =>
        _db.Set<AllowedPunchNetwork>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);

    public Task<AllowedPunchNetwork?> GetNetworkAsync(int networkId, CancellationToken ct) =>
        _db.Set<AllowedPunchNetwork>().FirstOrDefaultAsync(x => x.NetworkId == networkId, ct);

    public Task<bool> NetworkExistsAsync(string ipOrCidr, int? excludeNetworkId, CancellationToken ct) =>
        _db.Set<AllowedPunchNetwork>().AnyAsync(x => x.IpOrCidr == ipOrCidr && (excludeNetworkId == null || x.NetworkId != excludeNetworkId), ct);

    public async Task<AllowedPunchNetwork> AddNetworkAsync(AllowedPunchNetwork network, CancellationToken ct)
    {
        _db.Set<AllowedPunchNetwork>().Add(network);
        await _db.SaveChangesAsync(ct);
        return network;
    }

    public Task UpdateNetworkAsync(AllowedPunchNetwork network, CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<bool> DeleteNetworkAsync(int networkId, CancellationToken ct) =>
        await _db.Set<AllowedPunchNetwork>().Where(x => x.NetworkId == networkId).ExecuteDeleteAsync(ct) > 0;

    // ── Autorizaciones de picada externa (historial) ────────────────────────

    public Task<EmployeePunchPolicy?> GetEffectivePolicyAsync(int employeeId, DateTime today, CancellationToken ct) =>
        _db.Set<EmployeePunchPolicy>().AsNoTracking()
            .Where(x => (x.EmployeeId == employeeId || x.IsGlobal) && x.AllowExternalPunch && x.RevokedAt == null
                && (x.ValidFrom == null || x.ValidFrom <= today) && (x.ValidTo == null || x.ValidTo >= today))
            .OrderBy(x => x.IsGlobal).ThenByDescending(x => x.PolicyId)   // la individual primero
            .FirstOrDefaultAsync(ct);

    public Task<EmployeePunchPolicy?> GetPolicyAsync(int policyId, CancellationToken ct) =>
        _db.Set<EmployeePunchPolicy>().FirstOrDefaultAsync(x => x.PolicyId == policyId, ct);

    public Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken ct) =>
        _db.Set<Employees>().AnyAsync(e => e.EmployeeId == employeeId, ct);

    public async Task<(List<PunchPolicyItemDto> Items, long Total)> GetPoliciesPagedAsync(
        string? search, bool onlyEffective, int? employeeId, Guid? batchId, DateTime today, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<EmployeePunchPolicy>().AsNoTracking().AsQueryable();
        if (employeeId.HasValue) query = query.Where(p => p.EmployeeId == employeeId.Value);
        if (batchId.HasValue) query = query.Where(p => p.BatchId == batchId.Value);
        if (onlyEffective)
        {
            query = query.Where(p => p.AllowExternalPunch && p.RevokedAt == null
                && (p.ValidFrom == null || p.ValidFrom <= today) && (p.ValidTo == null || p.ValidTo >= today));
        }

        // Left join (la autorización global no tiene empleado) directo a Employees + People + Departments:
        // la vista vw_EmployeeDetails era costosa para solo nombre, cédula y departamento.
        var joined =
            from p in query
            join e in _db.Set<Employees>().AsNoTracking() on p.EmployeeId equals (int?)e.EmployeeId into ej
            from e in ej.DefaultIfEmpty()
            join pe in _db.Set<People>().AsNoTracking() on (int?)e.PersonID equals (int?)pe.PersonId into pj
            from pe in pj.DefaultIfEmpty()
            join d in _db.Set<Departments>().AsNoTracking() on e.DepartmentId equals (int?)d.DepartmentId into dj
            from d in dj.DefaultIfEmpty()
            select new { p, FirstName = (string?)pe.FirstName, LastName = (string?)pe.LastName, IdCard = (string?)pe.IdCard, Department = (string?)d.Name };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            // En la autorización global no hay persona: el LIKE queda NULL y se excluye sola
            joined = joined.Where(x => x.FirstName!.Contains(s) || x.LastName!.Contains(s) || x.IdCard!.Contains(s));
        }

        var total = await joined.LongCountAsync(ct);
        var rows = await joined
            .OrderByDescending(x => x.p.IsGlobal).ThenBy(x => x.LastName).ThenBy(x => x.FirstName).ThenByDescending(x => x.p.PolicyId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var items = await ToItemsAsync(rows.Select(r => (r.p, r.FirstName, r.LastName, r.IdCard, r.Department)).ToList(), today, ct);
        return (items, total);
    }

    public async Task<PunchPolicyItemDto?> GetPolicyItemAsync(int policyId, DateTime today, CancellationToken ct)
    {
        var r = await (
            from p in _db.Set<EmployeePunchPolicy>().AsNoTracking()
            join e in _db.Set<Employees>().AsNoTracking() on p.EmployeeId equals (int?)e.EmployeeId into ej
            from e in ej.DefaultIfEmpty()
            join pe in _db.Set<People>().AsNoTracking() on (int?)e.PersonID equals (int?)pe.PersonId into pj
            from pe in pj.DefaultIfEmpty()
            join d in _db.Set<Departments>().AsNoTracking() on e.DepartmentId equals (int?)d.DepartmentId into dj
            from d in dj.DefaultIfEmpty()
            where p.PolicyId == policyId
            select new { p, FirstName = (string?)pe.FirstName, LastName = (string?)pe.LastName, IdCard = (string?)pe.IdCard, Department = (string?)d.Name }).FirstOrDefaultAsync(ct);
        if (r is null) return null;
        return (await ToItemsAsync(new List<(EmployeePunchPolicy, string?, string?, string?, string?)> { (r.p, r.FirstName, r.LastName, r.IdCard, r.Department) }, today, ct))[0];
    }

    // Completa nombres de quien autorizó / revocó y del departamento del lote (una consulta por catálogo, no por fila).
    private async Task<List<PunchPolicyItemDto>> ToItemsAsync(
        List<(EmployeePunchPolicy P, string? FirstName, string? LastName, string? IdCard, string? Department)> rows, DateTime today, CancellationToken ct)
    {
        var userIds = rows.SelectMany(r => new[] { r.P.AuthorizedBy, r.P.RevokedBy }).Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var names = userIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Set<Employees>().AsNoTracking().Where(e => userIds.Contains(e.EmployeeId))
                .Join(_db.Set<People>().AsNoTracking(), e => e.PersonID, pe => pe.PersonId, (e, pe) => new { e.EmployeeId, pe.LastName, pe.FirstName })
                .ToDictionaryAsync(x => x.EmployeeId, x => $"{x.LastName} {x.FirstName}".Trim(), ct);
        var deptIds = rows.Where(r => r.P.BatchDepartmentId.HasValue).Select(r => r.P.BatchDepartmentId!.Value).Distinct().ToList();
        var depts = deptIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Set<Departments>().AsNoTracking().Where(d => deptIds.Contains(d.DepartmentId)).ToDictionaryAsync(d => d.DepartmentId, d => d.Name, ct);

        string? NameOf(int? id) => id.HasValue && names.TryGetValue(id.Value, out var n) ? n : null;
        return rows.Select(r =>
        {
            var status = StatusOf(r.P, today);
            return new PunchPolicyItemDto
            {
                PolicyId = r.P.PolicyId,
                EmployeeId = r.P.EmployeeId,
                IsGlobal = r.P.IsGlobal,
                FullName = r.P.IsGlobal ? "Toda la institución" : $"{r.LastName} {r.FirstName}".Trim(),
                IdCard = r.IdCard,
                Department = r.Department,
                Reason = r.P.Reason,
                ValidFrom = r.P.ValidFrom,
                ValidTo = r.P.ValidTo,
                Status = status,
                IsEffective = status == "ACTIVE",
                AuthorizedByName = NameOf(r.P.AuthorizedBy),
                CreatedAt = r.P.CreatedAt,
                RevokedAt = r.P.RevokedAt,
                RevokedByName = NameOf(r.P.RevokedBy),
                RevokeReason = r.P.RevokeReason,
                BatchId = r.P.BatchId,
                BatchDepartmentName = r.P.BatchDepartmentId.HasValue && depts.TryGetValue(r.P.BatchDepartmentId.Value, out var dn) ? dn : null
            };
        }).ToList();
    }

    public async Task<HashSet<int>> GetEmployeesWithOverlapAsync(
        IReadOnlyCollection<int> employeeIds, DateTime? from, DateTime? to, int? excludePolicyId, CancellationToken ct)
    {
        var result = new HashSet<int>();
        // Lotes de 1000 ids para no superar el límite de parámetros de SQL Server
        foreach (var chunk in employeeIds.Chunk(1000))
        {
            var ids = chunk.ToList();
            var found = await _db.Set<EmployeePunchPolicy>().AsNoTracking()
                .Where(p => p.EmployeeId != null && ids.Contains(p.EmployeeId.Value) && p.AllowExternalPunch && p.RevokedAt == null
                    && (excludePolicyId == null || p.PolicyId != excludePolicyId)
                    && (to == null || p.ValidFrom == null || p.ValidFrom <= to)
                    && (from == null || p.ValidTo == null || p.ValidTo >= from))
                .Select(p => p.EmployeeId!.Value).Distinct().ToListAsync(ct);
            result.UnionWith(found);
        }
        return result;
    }

    public Task<bool> HasGlobalOverlapAsync(DateTime? from, DateTime? to, CancellationToken ct) =>
        _db.Set<EmployeePunchPolicy>().AsNoTracking().AnyAsync(p => p.IsGlobal && p.AllowExternalPunch && p.RevokedAt == null
            && (to == null || p.ValidFrom == null || p.ValidFrom <= to)
            && (from == null || p.ValidTo == null || p.ValidTo >= from), ct);

    public async Task AddPoliciesAsync(IReadOnlyCollection<EmployeePunchPolicy> policies, CancellationToken ct)
    {
        _db.Set<EmployeePunchPolicy>().AddRange(policies);
        await _db.SaveChangesAsync(ct);
    }

    public Task SavePoliciesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public Task<List<EmployeePunchPolicy>> GetOpenPoliciesOfBatchAsync(Guid batchId, CancellationToken ct) =>
        _db.Set<EmployeePunchPolicy>().Where(p => p.BatchId == batchId && p.RevokedAt == null).ToListAsync(ct);

    public Task<string?> GetDepartmentNameAsync(int departmentId, CancellationToken ct) =>
        _db.Set<Departments>().AsNoTracking().Where(d => d.DepartmentId == departmentId && d.IsActive).Select(d => d.Name).FirstOrDefaultAsync(ct);

    public async Task<(List<int> EmployeeIds, int Departments)> GetEmployeesOfDepartmentAsync(int departmentId, bool includeChildren, CancellationToken ct)
    {
        var ids = new HashSet<int> { departmentId };
        if (includeChildren)
        {
            // Catálogo pequeño: se recorre el árbol en memoria
            var all = await _db.Set<Departments>().AsNoTracking().Where(d => d.IsActive).Select(d => new { d.DepartmentId, d.ParentId }).ToListAsync(ct);
            var frontier = new List<int> { departmentId };
            while (frontier.Count > 0)
            {
                var next = all.Where(d => d.ParentId.HasValue && frontier.Contains(d.ParentId.Value) && ids.Add(d.DepartmentId)).Select(d => d.DepartmentId).ToList();
                frontier = next;
            }
        }
        var idList = ids.ToList();
        var employees = await _db.vwEmployeeDetails.AsNoTracking()
            .Where(e => e.DepartmentID != null && idList.Contains(e.DepartmentID.Value))
            .Select(e => e.EmployeeID).Distinct().ToListAsync(ct);
        return (employees, idList.Count);
    }

    private static string StatusOf(EmployeePunchPolicy p, DateTime today)
    {
        if (p.RevokedAt != null || !p.AllowExternalPunch) return "REVOKED";
        if (p.ValidFrom != null && p.ValidFrom.Value.Date > today.Date) return "SCHEDULED";
        if (p.ValidTo != null && p.ValidTo.Value.Date < today.Date) return "EXPIRED";
        return "ACTIVE";
    }

    // ── Registro ────────────────────────────────────────────────────────────

    public async Task AddLogAsync(PunchAccessLog log, CancellationToken ct)
    {
        _db.Set<PunchAccessLog>().Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(List<PunchAccessLogItemDto> Items, long Total)> GetLogPagedAsync(
        DateTime? from, DateTime? to, int? employeeId, string? result, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Set<PunchAccessLog>().AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.AttemptedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(x => x.AttemptedAt < to.Value.Date.AddDays(1));
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(result)) query = query.Where(x => x.Result == result);

        var total = await query.LongCountAsync(ct);
        var rows = await (
            from l in query.OrderByDescending(x => x.AttemptedAt).Skip((page - 1) * pageSize).Take(pageSize)
            join e in _db.Set<Employees>().AsNoTracking() on l.EmployeeId equals e.EmployeeId into ej
            from e in ej.DefaultIfEmpty()
            join pe in _db.Set<People>().AsNoTracking() on (int?)e.PersonID equals (int?)pe.PersonId into pj
            from pe in pj.DefaultIfEmpty()
            select new
            {
                l.LogId, l.EmployeeId, l.AttemptedAt, l.IpAddress, l.ForwardedFor, l.Result, l.Mode, l.Detail,
                First = (string?)pe.FirstName, Last = (string?)pe.LastName
            }).ToListAsync(ct);

        var items = rows.Select(r => new PunchAccessLogItemDto
        {
            LogId = r.LogId,
            EmployeeId = r.EmployeeId,
            EmployeeName = $"{r.Last} {r.First}".Trim(),
            AttemptedAt = r.AttemptedAt,
            IpAddress = r.IpAddress,
            ForwardedFor = r.ForwardedFor,
            Result = r.Result,
            Mode = r.Mode,
            Detail = r.Detail
        }).ToList();
        return (items, total);
    }

    // ── Modo de operación ───────────────────────────────────────────────────

    public Task<string?> GetModeAsync(CancellationToken ct) => GetParameterAsync(ModeParameterName, ct);

    public Task<string?> GetParameterAsync(string name, CancellationToken ct) =>
        _db.Parameters.AsNoTracking().Where(p => p.Name == name && p.IsActive)
            .Select(p => p.Pvalues).FirstOrDefaultAsync(ct);

    public async Task SetModeAsync(string mode, CancellationToken ct)
    {
        var now = _clock.Now;
        var updated = await _db.Parameters.Where(p => p.Name == ModeParameterName)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Pvalues, mode).SetProperty(p => p.UpdatedAt, now), ct);
        if (updated == 0)
            throw new InvalidOperationException("Falta el parámetro PUNCH_NETWORK_MODE en HR.tbl_Parameters.");
    }
}
