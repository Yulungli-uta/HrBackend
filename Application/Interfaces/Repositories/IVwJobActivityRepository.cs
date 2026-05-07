using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Repositorio de solo lectura para la vista vw_JobActivity.</summary>
public interface IVwJobActivityRepository
{
    /// <summary>Retorna todas las actividades asignadas a cargos.</summary>
    Task<IEnumerable<VwJobActivity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Retorna las actividades de un cargo específico.</summary>
    Task<IEnumerable<VwJobActivity>> GetByJobAsync(int jobId, CancellationToken ct = default);

    /// <summary>Retorna solo las actividades con asignación activa.</summary>
    Task<IEnumerable<VwJobActivity>> GetActiveAssignmentsAsync(CancellationToken ct = default);

    /// <summary>Retorna las actividades del cargo filtrando solo asignaciones activas.</summary>
    Task<IEnumerable<VwJobActivity>> GetActiveByJobAsync(int jobId, CancellationToken ct = default);
}
