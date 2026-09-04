using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IJobActivityService : IService<JobActivity, int>
{
    Task<bool> DeleteByKeysAsync(int jobId, int activitiesId, CancellationToken ct);
}
