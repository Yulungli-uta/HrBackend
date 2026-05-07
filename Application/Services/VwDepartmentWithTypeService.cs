using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Services;

public class VwDepartmentWithTypeService : IVwDepartmentWithTypeService
{
    private readonly IVwDepartmentWithTypeRepository _repo;

    public VwDepartmentWithTypeService(IVwDepartmentWithTypeRepository repo) => _repo = repo;

    public Task<IEnumerable<VwDepartmentWithType>> GetAllAsync(CancellationToken ct = default) =>
        _repo.GetAllAsync(ct);

    public Task<IEnumerable<VwDepartmentWithType>> GetActiveAsync(CancellationToken ct = default) =>
        _repo.GetActiveAsync(ct);

    public Task<IEnumerable<VwDepartmentWithType>> GetByTypeAsync(int departmentTypeId, CancellationToken ct = default) =>
        _repo.GetByTypeAsync(departmentTypeId, ct);

    public Task<IEnumerable<VwDepartmentWithType>> GetByScopeAsync(int departmentScopeId, CancellationToken ct = default) =>
        _repo.GetByScopeAsync(departmentScopeId, ct);

    public Task<VwDepartmentWithType?> GetByIdAsync(int departmentId, CancellationToken ct = default) =>
        _repo.GetByIdAsync(departmentId, ct);
}
