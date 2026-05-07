using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Repositorio de solo lectura para la vista vw_JobWithDegreeAndGroup.</summary>
public interface IVwJobWithDegreeAndGroupRepository
{
    /// <summary>Retorna todos los cargos con su título y grupo ocupacional.</summary>
    Task<IEnumerable<VwJobWithDegreeAndGroup>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Retorna los cargos filtrados por grupo ocupacional.</summary>
    Task<IEnumerable<VwJobWithDegreeAndGroup>> GetByGroupAsync(int groupId, CancellationToken ct = default);

    /// <summary>Retorna los cargos que tienen un título activo.</summary>
    Task<IEnumerable<VwJobWithDegreeAndGroup>> GetWithActiveDegreeAsync(CancellationToken ct = default);

    /// <summary>Retorna el cargo cuyo ID coincide, o null si no existe.</summary>
    Task<VwJobWithDegreeAndGroup?> GetByIdAsync(int jobId, CancellationToken ct = default);
}
