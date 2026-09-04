using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class JobActivityService : Service<JobActivity, int>, IJobActivityService
{
    private readonly IJobActivityRepository _repository;

    public JobActivityService(IJobActivityRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public Task<bool> DeleteByKeysAsync(int jobId, int activitiesId, CancellationToken ct) =>
        _repository.DeleteByKeysAsync(jobId, activitiesId, ct);
}
