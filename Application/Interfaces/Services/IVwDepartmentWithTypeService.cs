using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Servicio de consulta para la vista vw_DepartmentWithType.</summary>
public interface IVwDepartmentWithTypeService
{
    Task<IEnumerable<VwDepartmentWithType>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<VwDepartmentWithType>> GetActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<VwDepartmentWithType>> GetByTypeAsync(int departmentTypeId, CancellationToken ct = default);
    Task<IEnumerable<VwDepartmentWithType>> GetByScopeAsync(int departmentScopeId, CancellationToken ct = default);
    Task<VwDepartmentWithType?> GetByIdAsync(int departmentId, CancellationToken ct = default);
}
