using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Servicio de consulta para la vista vw_JobWithDegreeAndGroup.</summary>
public interface IVwJobWithDegreeAndGroupService
{
    Task<IEnumerable<VwJobWithDegreeAndGroup>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<VwJobWithDegreeAndGroup>> GetByGroupAsync(int groupId, CancellationToken ct = default);
    Task<IEnumerable<VwJobWithDegreeAndGroup>> GetWithActiveDegreeAsync(CancellationToken ct = default);
    Task<VwJobWithDegreeAndGroup?> GetByIdAsync(int jobId, CancellationToken ct = default);
}
