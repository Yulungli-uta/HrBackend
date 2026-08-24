using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.DepartmentAuthority;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Servicio de negocio para la gestión de autoridades de departamento.
/// Extiende <see cref="BaseService{TEntity, TKey}"/> con lógica de dominio especializada.
/// Principio SRP: responsabilidad única de orquestar la lógica de negocio de DepartmentAuthority.
/// Principio OCP: abierto para extensión mediante la interfaz, cerrado para modificación.
/// </summary>
public class DepartmentAuthorityService
    : Service<DepartmentAuthority, int>, IDepartmentAuthorityService
{
    // HR.ref_Types (Category=AUTHORITY_TYPE) — únicos dos tipos que determinan ImmediateBossId,
    // misma regla que EmployeeProvisioningOrchestrator (Director tiene prioridad sobre Decano).
    // Se resuelven por Name, nunca por TypeId fijo (el TypeId es IDENTITY y varía entre ambientes).
    private const string AuthorityTypeCategory = "AUTHORITY_TYPE";
    private const string AuthorityTypeNameDirector = "Director";
    private const string AuthorityTypeNameDecano = "Decano";

    private readonly IDepartmentAuthorityRepository _authorityRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<DepartmentAuthorityService> _logger;
    private readonly AppDbContext _db;
    private readonly IRefTypesService _refTypes;

    public DepartmentAuthorityService(
        IDepartmentAuthorityRepository repository,
        IMapper mapper,
        ILogger<DepartmentAuthorityService> logger,
        AppDbContext db,
        IRefTypesService refTypes
        ) : base(repository)
    {
        _authorityRepository = repository;
        _mapper = mapper;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
    }

    // -------------------------------------------------------
    // Compatibilidad con el uso antiguo (entity) — mismo patrón que ContractsService:
    // hidewith `new` + relistar la interfaz para que el dispatch por IDepartmentAuthorityService
    // (como lo usa el controller) también pase por aquí.
    // -------------------------------------------------------

    /// <inheritdoc/>
    public new async Task<DepartmentAuthority> CreateAsync(DepartmentAuthority entity, CancellationToken ct)
    {
        var created = await base.CreateAsync(entity, ct);

        // Efecto secundario best-effort: si esto falla, la autoridad ya quedó creada igual
        // (nunca debe tumbar el alta de la autoridad por un problema de sincronización).
        try
        {
            await SyncImmediateBossIfVigenteAsync(created, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo sincronizar ImmediateBossId tras crear DepartmentAuthority {AuthorityId}",
                created.AuthorityId);
        }

        return created;
    }

    /// <summary>
    /// Cuando se registra una autoridad VIGENTE (no un histórico) de tipo Director/Decano,
    /// actualiza el ImmediateBossId de todo el personal activo de ese departamento (excepto
    /// la autoridad misma) al empleado que resulte ganador según la misma prioridad usada en
    /// <c>EmployeeProvisioningOrchestrator</c> (Director &gt; Decano). No dispara nada cuando el
    /// registro es histórico (IsActive=0 o EndDate ya vencido) — esos se insertan para dejar
    /// trazabilidad, no para reflejar al jefe actual.
    /// </summary>
    private async Task SyncImmediateBossIfVigenteAsync(DepartmentAuthority created, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var esVigente = created.IsActive
            && created.StartDate <= today
            && (created.EndDate == null || created.EndDate >= today);

        if (!esVigente)
            return;

        var authorityTypes = await _refTypes.GetByCategoryAsync(AuthorityTypeCategory, ct);
        var directorTypeId = authorityTypes.FirstOrDefault(t => t.Name == AuthorityTypeNameDirector)?.TypeId;
        var decanoTypeId = authorityTypes.FirstOrDefault(t => t.Name == AuthorityTypeNameDecano)?.TypeId;

        if (created.AuthorityTypeId != directorTypeId && created.AuthorityTypeId != decanoTypeId)
            return; // solo Director/Decano determinan ImmediateBossId

        // Recalcula el jefe vigente del departamento en vez de asumir que created.EmployeeId
        // ganó — evita pisar un Director vigente con un Decano recién insertado en el mismo depto.
        var resolvedBossId = await _db.DepartmentAuthorities
            .AsNoTracking()
            .Where(a => a.DepartmentId == created.DepartmentId
                     && a.IsActive
                     && (a.AuthorityTypeId == directorTypeId || a.AuthorityTypeId == decanoTypeId)
                     && a.StartDate <= today
                     && (a.EndDate == null || a.EndDate >= today))
            .OrderBy(a => a.AuthorityTypeId == directorTypeId ? 1 : 2)
            .Select(a => (int?)a.EmployeeId)
            .FirstOrDefaultAsync(ct);

        if (resolvedBossId is null)
            return;

        var affectedIds = await _db.Employees
            .Where(e => e.DepartmentId == created.DepartmentId
                     && e.IsActive
                     && e.EmployeeId != resolvedBossId.Value
                     && e.ImmediateBossId != resolvedBossId.Value)
            .Select(e => e.EmployeeId)
            .ToListAsync(ct);

        if (affectedIds.Count == 0)
            return;

        await _db.Employees
            .Where(e => affectedIds.Contains(e.EmployeeId))
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.ImmediateBossId, resolvedBossId.Value), ct);

        _logger.LogInformation(
            "ImmediateBossId sincronizado a {BossId} para {Count} empleados del departamento {DepartmentId} tras la autoridad vigente {AuthorityId}",
            resolvedBossId.Value, affectedIds.Count, created.DepartmentId, created.AuthorityId);
    }

    /// <inheritdoc/>
    public Task<PagedResult<DepartmentAuthority>> GetPagedByDepartmentAsync(
        int departmentId,
        int page,
        int pageSize,
        CancellationToken ct,
        bool onlyActive = false) =>
        _authorityRepository.GetPagedByDepartmentAsync(departmentId, page, pageSize, ct, onlyActive);

    /// <inheritdoc/>
    public Task<PagedResult<DepartmentAuthority>> GetPagedByEmployeeAsync(
        int employeeId,
        int page,
        int pageSize,
        CancellationToken ct) =>
        _authorityRepository.GetPagedByEmployeeAsync(employeeId, page, pageSize, ct);

    /// <inheritdoc/>
    public Task<List<DepartmentAuthority>> GetActiveByDepartmentAsync(
        int departmentId,
        CancellationToken ct) =>
        _authorityRepository.GetActiveByDepartmentAsync(departmentId, ct);

    /// <inheritdoc/>
    public async Task<DepartmentAuthorityDenominationDto?> GetDenominationByIdCardAsync(
        string idCard,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idCard))
            return null;

        _logger.LogInformation($"Consultado GetDenominationByIdCardAsync cedula: {idCard}");
        var authority = await _authorityRepository.GetActiveAuthorityByIdCardAsync(idCard.Trim(), ct);
        _logger.LogInformation($"Consultado GetDenominationByIdCardAsync authority: {authority}");
        // Si no hay autoridad activa, buscamos al empleado de todas formas para retornar sus datos básicos
        if (authority == null)
        {
            // Retornamos null — el controller decidirá si retornar 404 o un DTO vacío
            return null;
        }

        var person = authority.Employee?.People;
        var fullName = person != null
            ? $"{person.LastName} {person.FirstName}".Trim()
            : "Sin nombre";

        return new DepartmentAuthorityDenominationDto
        {
            IdCard = idCard,
            EmployeeId = authority.EmployeeId,
            EmployeeFullName = fullName,
            EmployeeEmail = person?.Email ?? authority.Employee?.Email,
            AuthorityId = authority.AuthorityId,
            Denomination = authority.Denomination,
            AuthorityTypeName = authority.AuthorityType?.Name,
            DepartmentName = authority.Department?.Name,
            DepartmentCode = authority.Department?.Code,
            StartDate = authority.StartDate,
            ResolutionCode = authority.ResolutionCode,
            HasActiveAuthority = true
        };
    }

    /// <inheritdoc/>
    public Task<PagedResult<DepartmentAuthority>> GetPagedWithSearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct,
        bool onlyActive = false) =>
        _authorityRepository.GetPagedWithSearchAsync(search, page, pageSize, ct, onlyActive);
}
