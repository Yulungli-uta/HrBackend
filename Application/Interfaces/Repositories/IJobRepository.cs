using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IJobRepository : IRepository<Job, int> {

        Task<IEnumerable<Job>> GetActiveJobsAsync( CancellationToken ct);

        Task<IEnumerable<Job>> SearchJobsByTitleAsync(String title, CancellationToken ct);
    }
   
    
}
