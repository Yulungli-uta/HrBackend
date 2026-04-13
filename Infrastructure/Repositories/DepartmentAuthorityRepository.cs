using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de autoridades de departamento.
/// Extiende <see cref="ServiceAwareEfRepository{TEntity, TKey}"/> con consultas especializadas
/// que incluyen eager loading de las entidades relacionadas para evitar N+1.
/// Principio SRP: responsabilidad única de acceso a datos para DepartmentAuthority.
/// </summary>
public class DepartmentAuthorityRepository
    : ServiceAwareEfRepository<DepartmentAuthority, int>, IDepartmentAuthorityRepository
{
    private readonly ILogger<DepartmentAuthorityRepository> _logger;
    public DepartmentAuthorityRepository(
        AppDbContext db,
        ILogger<DepartmentAuthorityRepository> logger
        ) : base(db) 
    {
        _logger = logger;        
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Consulta base con eager loading
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna el IQueryable base con todas las entidades relacionadas incluidas.
    /// Centraliza el eager loading para evitar duplicación (principio DRY).
    /// </summary>
    private IQueryable<DepartmentAuthority> QueryWithIncludes() =>
        _db.DepartmentAuthorities
            .AsNoTracking()
            .Include(a => a.Department)
            .Include(a => a.Employee)
                .ThenInclude(e => e!.People)
            .Include(a => a.AuthorityType)
            .Include(a => a.Job);

    // ─────────────────────────────────────────────────────────────────────────
    // Métodos especializados
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<PagedResult<DepartmentAuthority>> GetPagedByDepartmentAsync(
        int departmentId,
        int page,
        int pageSize,
        CancellationToken ct,
        bool onlyActive = false)
    {
        var query = QueryWithIncludes()
            .Where(a => a.DepartmentId == departmentId);

        if (onlyActive)
            query = query.Where(a => a.IsActive && a.EndDate == null);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<DepartmentAuthority>> GetPagedByEmployeeAsync(
        int employeeId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = QueryWithIncludes()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.StartDate);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    /// <inheritdoc/>
    public async Task<List<DepartmentAuthority>> GetActiveByDepartmentAsync(
        int departmentId,
        CancellationToken ct) =>
        await QueryWithIncludes()
            .Where(a => a.DepartmentId == departmentId && a.IsActive && a.EndDate == null)
            .OrderBy(a => a.AuthorityTypeId)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<DepartmentAuthority?> GetActiveAuthorityByIdCardAsync(
        string idCard,
        CancellationToken ct)
    {

        DateOnly hoy = DateOnly.FromDateTime(DateTime.Now);

        //DepartmentAuthority? authority = await _db.DepartmentAuthorities.AsNoTracking()
        //    .Include(a => a.Department)
        //    .Include(a => a.AuthorityType)
        //    .Include(a => a.Job)
        //    .Include(a => a.Employee)
        //        .ThenInclude(e => e!.People)
        //    .Where(a =>
        //         a.IsActive &&
        //         a.Employee.IsActive &&
        //         a.Employee.People.IsActive &&
        //         a.Employee.People.IdCard == idCard &&
        //         a.StartDate <= hoy &&
        //         (a.EndDate == null || (a.EndDate >= a.StartDate && a.EndDate >= hoy)))
        //    .OrderByDescending(a => a.StartDate)
        //    .FirstOrDefaultAsync(ct);

        //var options = new JsonSerializerOptions
        //{
        //    WriteIndented = true,
        //    ReferenceHandler = ReferenceHandler.IgnoreCycles
        //};

        //string jsonAuthority = JsonSerializer.Serialize(authority, options);

        //_logger.LogInformation($"*************Datos Autoridad encontrado: " +
        //    $"{authority.Employee.People.FirstName}, " +
        //    $" cedula: {authority.Employee.People.IdCard}," +
        //    $" json: {jsonAuthority}");

        //return authority;

        // Join: People → Employees → DepartmentAuthorities (solo la autoridad activa más reciente)
        return await _db.DepartmentAuthorities
            .AsNoTracking()
            .Include(a => a.Department)
            .Include(a => a.AuthorityType)
            .Include(a => a.Job)
            .Include(a => a.Employee)
                .ThenInclude(e => e!.People)
             .Where(a =>
                 a.IsActive &&
                 a.Employee.IsActive &&
                 a.Employee.People.IsActive &&
                 a.Employee.People.IdCard == idCard &&
                 a.StartDate <= hoy &&
                 (a.EndDate == null || (a.EndDate >= a.StartDate && a.EndDate >= hoy)))
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<DepartmentAuthority>> GetPagedWithSearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct,
        bool onlyActive = false)
    {
        var query = QueryWithIncludes();

        if (onlyActive)
            query = query.Where(a => a.IsActive && a.EndDate == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                (a.Denomination != null && a.Denomination.ToLower().Contains(term)) ||
                (a.ResolutionCode != null && a.ResolutionCode.ToLower().Contains(term)) ||
                (a.Notes != null && a.Notes.ToLower().Contains(term)) ||
                (a.Department != null && a.Department.Name.ToLower().Contains(term)) ||
                (a.Employee != null && a.Employee.People != null &&
                    (a.Employee.People.FirstName.ToLower().Contains(term) ||
                     a.Employee.People.LastName.ToLower().Contains(term) ||
                     a.Employee.People.IdCard.ToLower().Contains(term))));
        }

        query = query.OrderByDescending(a => a.StartDate);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper privado de paginación
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Materializa un IQueryable en un PagedResult aplicando skip/take.
    /// Principio DRY: evita duplicar la lógica de paginación en cada método.
    /// </summary>
    private static async Task<PagedResult<DepartmentAuthority>> ToPagedResultAsync(
        IQueryable<DepartmentAuthority> query,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<DepartmentAuthority>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
