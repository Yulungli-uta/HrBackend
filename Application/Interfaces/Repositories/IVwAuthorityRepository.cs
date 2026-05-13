using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>
/// Repositorio de solo lectura para la vista HR.vw_Authority.
/// Expone consultas desnormalizadas de autoridades con datos de persona, departamento,
/// tipo de autoridad y cargo ya resueltos por la vista SQL.
/// </summary>
public interface IVwAuthorityRepository
{
    /// <summary>Retorna todos los registros de la vista sin paginación.</summary>
    Task<IEnumerable<VwAuthority>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Retorna solo las autoridades activas y vigentes (IsActive = true y EndDate IS NULL).</summary>
    Task<IEnumerable<VwAuthority>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Retorna las autoridades de un departamento específico.</summary>
    /// <param name="departmentId">ID del departamento.</param>
    Task<IEnumerable<VwAuthority>> GetByDepartmentAsync(int departmentId, CancellationToken ct = default);

    /// <summary>Retorna el historial de autoridades de un empleado específico.</summary>
    /// <param name="employeeId">ID del empleado.</param>
    Task<IEnumerable<VwAuthority>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Retorna un registro por su AuthorityID, o null si no existe.</summary>
    /// <param name="authorityId">ID de la autoridad.</param>
    Task<VwAuthority?> GetByIdAsync(int authorityId, CancellationToken ct = default);

    /// <summary>
    /// Retorna un resultado paginado con búsqueda de texto libre.
    /// Busca en: nombre del empleado, cédula, nombre del departamento, código del departamento,
    /// denominación, código de resolución y nombre del tipo de autoridad.
    /// </summary>
    /// <param name="search">Texto de búsqueda (null o vacío = sin filtro).</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Registros por página.</param>
    /// <param name="onlyActive">Si true, solo registros activos y vigentes.</param>
    Task<PagedResult<VwAuthority>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        bool onlyActive,
        CancellationToken ct);
}
