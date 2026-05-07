using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Servicio de consulta para la vista vw_JobActivity.</summary>
public interface IVwJobActivityService
{
    Task<IEnumerable<VwJobActivity>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<VwJobActivity>> GetByJobAsync(int jobId, CancellationToken ct = default);
    Task<IEnumerable<VwJobActivity>> GetActiveAssignmentsAsync(CancellationToken ct = default);
    Task<IEnumerable<VwJobActivity>> GetActiveByJobAsync(int jobId, CancellationToken ct = default);
}
