using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de solo lectura para la vista HR.vw_Authority.
/// Principio SRP: responsabilidad única de acceso de lectura a la vista de autoridades.
/// </summary>
public class VwAuthorityRepository : IVwAuthorityRepository
{
    private readonly AppDbContext _db;

    public VwAuthorityRepository(AppDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<IEnumerable<VwAuthority>> GetAllAsync(CancellationToken ct = default) =>
        await _db.VwAuthority.AsNoTracking()
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IEnumerable<VwAuthority>> GetActiveAsync(CancellationToken ct = default) =>
        await _db.VwAuthority.AsNoTracking()
            .Where(a => a.IsActive && a.EndDate == null)
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IEnumerable<VwAuthority>> GetByDepartmentAsync(
        int departmentId, CancellationToken ct = default) =>
        await _db.VwAuthority.AsNoTracking()
            .Where(a => a.DepartmentID == departmentId)
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IEnumerable<VwAuthority>> GetByEmployeeAsync(
        int employeeId, CancellationToken ct = default) =>
        await _db.VwAuthority.AsNoTracking()
            .Where(a => a.EmployeeID == employeeId)
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<VwAuthority?> GetByIdAsync(int authorityId, CancellationToken ct = default) =>
        await _db.VwAuthority.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AuthorityID == authorityId, ct);

    /// <inheritdoc/>
    public async Task<PagedResult<VwAuthority>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        bool onlyActive,
        CancellationToken ct)
    {
        var query = _db.VwAuthority.AsNoTracking();

        if (onlyActive)
            query = query.Where(a => a.IsActive && a.EndDate == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.EmployeeFullName.ToLower().Contains(term) ||
                a.EmployeeIDCard.ToLower().Contains(term) ||
                a.DepartmentName.ToLower().Contains(term) ||
                a.DepartmentCode.ToLower().Contains(term) ||
                a.AuthorityTypeName.ToLower().Contains(term) ||
                (a.Denomination != null && a.Denomination.ToLower().Contains(term)) ||
                (a.ResolutionCode != null && a.ResolutionCode.ToLower().Contains(term)));
        }

        query = query.OrderByDescending(a => a.StartDate);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<VwAuthority>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
