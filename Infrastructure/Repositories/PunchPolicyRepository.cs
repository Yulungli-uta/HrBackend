using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.PunchPolicy;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class PunchPolicyRepository : IPunchPolicyRepository
{
    private const string ModeParameterName = "PUNCH_NETWORK_MODE";
    private readonly AppDbContext _db;

    public PunchPolicyRepository(AppDbContext db) => _db = db;

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

    // ── Permisos de picada externa ──────────────────────────────────────────

    public Task<EmployeePunchPolicy?> GetPolicyAsync(int employeeId, CancellationToken ct) =>
        _db.Set<EmployeePunchPolicy>().FirstOrDefaultAsync(x => x.EmployeeId == employeeId, ct);

    public Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken ct) =>
        _db.Set<Employees>().AnyAsync(e => e.EmployeeId == employeeId, ct);

    public async Task<(List<PunchPolicyItemDto> Items, long Total)> GetPoliciesPagedAsync(
        string? search, bool onlyEffective, DateTime today, int page, int pageSize, CancellationToken ct)
    {
        var query =
            from p in _db.Set<EmployeePunchPolicy>().AsNoTracking()
            join e in _db.vwEmployeeDetails.AsNoTracking() on p.EmployeeId equals e.EmployeeID
            select new { p, e };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.e.FirstName.Contains(s) || x.e.LastName.Contains(s) || x.e.IDCard.Contains(s));
        }
        if (onlyEffective)
        {
            query = query.Where(x => x.p.AllowExternalPunch
                && (x.p.ValidFrom == null || x.p.ValidFrom <= today)
                && (x.p.ValidTo == null || x.p.ValidTo >= today));
        }

        var total = await query.LongCountAsync(ct);
        var rows = await query
            .OrderBy(x => x.e.LastName).ThenBy(x => x.e.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.p.EmployeeId, x.e.FirstName, x.e.LastName, x.e.IDCard, x.e.Department,
                x.p.AllowExternalPunch, x.p.Reason, x.p.ValidFrom, x.p.ValidTo, x.p.UpdatedAt, x.p.CreatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new PunchPolicyItemDto
        {
            EmployeeId = r.EmployeeId,
            FullName = $"{r.LastName} {r.FirstName}".Trim(),
            IdCard = r.IDCard,
            Department = r.Department,
            AllowExternalPunch = r.AllowExternalPunch,
            Reason = r.Reason,
            ValidFrom = r.ValidFrom,
            ValidTo = r.ValidTo,
            IsEffective = IsEffective(r.AllowExternalPunch, r.ValidFrom, r.ValidTo, today),
            UpdatedAt = r.UpdatedAt ?? r.CreatedAt
        }).ToList();
        return (items, total);
    }

    public async Task<PunchPolicyItemDto?> GetPolicyItemAsync(int employeeId, DateTime today, CancellationToken ct)
    {
        var r = await (
            from p in _db.Set<EmployeePunchPolicy>().AsNoTracking()
            join e in _db.vwEmployeeDetails.AsNoTracking() on p.EmployeeId equals e.EmployeeID
            where p.EmployeeId == employeeId
            select new
            {
                p.EmployeeId, e.FirstName, e.LastName, e.IDCard, e.Department,
                p.AllowExternalPunch, p.Reason, p.ValidFrom, p.ValidTo, p.UpdatedAt, p.CreatedAt
            }).FirstOrDefaultAsync(ct);
        if (r is null) return null;
        return new PunchPolicyItemDto
        {
            EmployeeId = r.EmployeeId,
            FullName = $"{r.LastName} {r.FirstName}".Trim(),
            IdCard = r.IDCard,
            Department = r.Department,
            AllowExternalPunch = r.AllowExternalPunch,
            Reason = r.Reason,
            ValidFrom = r.ValidFrom,
            ValidTo = r.ValidTo,
            IsEffective = IsEffective(r.AllowExternalPunch, r.ValidFrom, r.ValidTo, today),
            UpdatedAt = r.UpdatedAt ?? r.CreatedAt
        };
    }

    public async Task UpsertPolicyAsync(EmployeePunchPolicy policy, CancellationToken ct)
    {
        if (policy.PolicyId == 0) _db.Set<EmployeePunchPolicy>().Add(policy);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeletePolicyAsync(int employeeId, CancellationToken ct) =>
        await _db.Set<EmployeePunchPolicy>().Where(x => x.EmployeeId == employeeId).ExecuteDeleteAsync(ct) > 0;

    private static bool IsEffective(bool allow, DateTime? from, DateTime? to, DateTime today) =>
        allow && (from == null || from.Value.Date <= today.Date) && (to == null || to.Value.Date >= today.Date);

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
            join e in _db.vwEmployeeDetails.AsNoTracking() on l.EmployeeId equals e.EmployeeID into ej
            from e in ej.DefaultIfEmpty()
            select new
            {
                l.LogId, l.EmployeeId, l.AttemptedAt, l.IpAddress, l.ForwardedFor, l.Result, l.Mode, l.Detail,
                First = e == null ? null : e.FirstName, Last = e == null ? null : e.LastName
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
        var now = DateTime.Now;
        var updated = await _db.Parameters.Where(p => p.Name == ModeParameterName)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Pvalues, mode).SetProperty(p => p.UpdatedAt, now), ct);
        if (updated == 0)
            throw new InvalidOperationException("Falta el parámetro PUNCH_NETWORK_MODE en HR.tbl_Parameters.");
    }
}
