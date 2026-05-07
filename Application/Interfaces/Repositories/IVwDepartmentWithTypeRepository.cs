using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Repositorio de solo lectura para la vista vw_DepartmentWithType.</summary>
public interface IVwDepartmentWithTypeRepository
{
    /// <summary>Retorna todos los departamentos con su tipo.</summary>
    Task<IEnumerable<VwDepartmentWithType>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Retorna solo los departamentos activos.</summary>
    Task<IEnumerable<VwDepartmentWithType>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Retorna los departamentos filtrados por tipo.</summary>
    Task<IEnumerable<VwDepartmentWithType>> GetByTypeAsync(int departmentTypeId, CancellationToken ct = default);

    /// <summary>Retorna los departamentos filtrados por ámbito.</summary>
    Task<IEnumerable<VwDepartmentWithType>> GetByScopeAsync(int departmentScopeId, CancellationToken ct = default);

    /// <summary>Retorna el departamento cuyo ID coincide, o null si no existe.</summary>
    Task<VwDepartmentWithType?> GetByIdAsync(int departmentId, CancellationToken ct = default);
}
