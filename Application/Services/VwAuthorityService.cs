using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Servicio de negocio para la vista HR.vw_Authority.
/// Principio SRP: orquesta únicamente las consultas de lectura de la vista de autoridades.
/// </summary>
public class VwAuthorityService : IVwAuthorityService
{
    private readonly IVwAuthorityRepository _repo;

    public VwAuthorityService(IVwAuthorityRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public Task<IEnumerable<VwAuthority>> GetAllAsync(CancellationToken ct = default) =>
        _repo.GetAllAsync(ct);

    /// <inheritdoc/>
    public Task<IEnumerable<VwAuthority>> GetActiveAsync(CancellationToken ct = default) =>
        _repo.GetActiveAsync(ct);

    /// <inheritdoc/>
    public Task<IEnumerable<VwAuthority>> GetByDepartmentAsync(int departmentId, CancellationToken ct = default) =>
        _repo.GetByDepartmentAsync(departmentId, ct);

    /// <inheritdoc/>
    public Task<IEnumerable<VwAuthority>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default) =>
        _repo.GetByEmployeeAsync(employeeId, ct);

    /// <inheritdoc/>
    public Task<VwAuthority?> GetByIdAsync(int authorityId, CancellationToken ct = default) =>
        _repo.GetByIdAsync(authorityId, ct);

    /// <inheritdoc/>
    public Task<PagedResult<VwAuthority>> GetPagedAsync(
        string? search, int page, int pageSize, bool onlyActive, CancellationToken ct) =>
        _repo.GetPagedAsync(search, page, pageSize, onlyActive, ct);
}
