using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Services;

public class VwJobWithDegreeAndGroupService : IVwJobWithDegreeAndGroupService
{
    private readonly IVwJobWithDegreeAndGroupRepository _repo;

    public VwJobWithDegreeAndGroupService(IVwJobWithDegreeAndGroupRepository repo) => _repo = repo;

    public Task<IEnumerable<VwJobWithDegreeAndGroup>> GetAllAsync(CancellationToken ct = default) =>
        _repo.GetAllAsync(ct);

    public Task<IEnumerable<VwJobWithDegreeAndGroup>> GetByGroupAsync(int groupId, CancellationToken ct = default) =>
        _repo.GetByGroupAsync(groupId, ct);

    public Task<IEnumerable<VwJobWithDegreeAndGroup>> GetWithActiveDegreeAsync(CancellationToken ct = default) =>
        _repo.GetWithActiveDegreeAsync(ct);

    public Task<VwJobWithDegreeAndGroup?> GetByIdAsync(int jobId, CancellationToken ct = default) =>
        _repo.GetByIdAsync(jobId, ct);
}
