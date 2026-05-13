using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Servicio de consulta para la vista HR.vw_Authority.
/// Principio ISP: expone solo los métodos de lectura que el dominio de autoridades requiere.
/// </summary>
public interface IVwAuthorityService
{
    /// <summary>Retorna todos los registros de la vista sin paginación.</summary>
    Task<IEnumerable<VwAuthority>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Retorna solo las autoridades activas y vigentes.</summary>
    Task<IEnumerable<VwAuthority>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Retorna las autoridades de un departamento específico.</summary>
    Task<IEnumerable<VwAuthority>> GetByDepartmentAsync(int departmentId, CancellationToken ct = default);

    /// <summary>Retorna el historial de autoridades de un empleado.</summary>
    Task<IEnumerable<VwAuthority>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Retorna un registro por su ID, o null si no existe.</summary>
    Task<VwAuthority?> GetByIdAsync(int authorityId, CancellationToken ct = default);

    /// <summary>Retorna un resultado paginado con búsqueda de texto libre.</summary>
    Task<PagedResult<VwAuthority>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        bool onlyActive,
        CancellationToken ct);
}
