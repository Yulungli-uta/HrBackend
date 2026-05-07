using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Services;

public class VwJobActivityService : IVwJobActivityService
{
    private readonly IVwJobActivityRepository _repo;

    public VwJobActivityService(IVwJobActivityRepository repo) => _repo = repo;

    public Task<IEnumerable<VwJobActivity>> GetAllAsync(CancellationToken ct = default) =>
        _repo.GetAllAsync(ct);

    public Task<IEnumerable<VwJobActivity>> GetByJobAsync(int jobId, CancellationToken ct = default) =>
        _repo.GetByJobAsync(jobId, ct);

    public Task<IEnumerable<VwJobActivity>> GetActiveAssignmentsAsync(CancellationToken ct = default) =>
        _repo.GetActiveAssignmentsAsync(ct);

    public Task<IEnumerable<VwJobActivity>> GetActiveByJobAsync(int jobId, CancellationToken ct = default) =>
        _repo.GetActiveByJobAsync(jobId, ct);
}
