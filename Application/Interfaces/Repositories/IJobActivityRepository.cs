using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IJobActivityRepository : IRepository<JobActivity, int>
{
    /// <summary>Elimina la asignación por su llave compuesta real (ActivitiesId, JobID) — el Id
    /// int genérico de IRepository no aplica a esta entidad, que no tiene un Id simple.</summary>
    Task<bool> DeleteByKeysAsync(int jobId, int activitiesId, CancellationToken ct);
}
