using AutoMapper;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.DepartmentAuthority;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
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
    private readonly IDepartmentAuthorityRepository _authorityRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<DepartmentAuthorityService> _logger;

    public DepartmentAuthorityService(
        IDepartmentAuthorityRepository repository,
        IMapper mapper,
        ILogger<DepartmentAuthorityService> logger
        ) : base(repository)        
    {
        _authorityRepository = repository;
        _mapper = mapper;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
            ? $"{person.FirstName} {person.LastName}".Trim()
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
