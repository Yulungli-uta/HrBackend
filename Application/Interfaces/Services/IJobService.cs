using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Jobs;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IJobService : IService<Job, int> {

        Task<IEnumerable<Job>> GetActiveJobsAsync(CancellationToken ct);

        Task<IEnumerable<Job>> SearchJobsByTitleAsync(String title, CancellationToken ct);
    }

}
