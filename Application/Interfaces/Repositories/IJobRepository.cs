using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IJobRepository : IRepository<Job, int> {

        Task<IEnumerable<Job>> GetActiveJobsAsync( CancellationToken ct);

        Task<IEnumerable<Job>> SearchJobsByTitleAsync(String title, CancellationToken ct);

        /// <summary>Indica si otro cargo activo ya usa esa descripción (se compara sin espacios sobrantes).</summary>
        Task<bool> ActiveDescriptionInUseAsync(string description, int? excludeJobId, CancellationToken ct);

        /// <summary>Nombre del tipo de cargo (ref_Types: LOES, LOSEP, Código Trabajo...), o null si no tiene.</summary>
        Task<string?> GetJobTypeNameAsync(int jobTypeId, CancellationToken ct);

        /// <summary>Indica si la escala ocupacional existe y está activa.</summary>
        Task<bool> IsScaleActiveAsync(int groupId, CancellationToken ct);
    }
   
    
}
