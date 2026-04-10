using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.DepartmentAuthority;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Contrato de servicio para la gestión de autoridades de departamento.
/// Extiende el servicio genérico con operaciones de negocio especializadas.
/// Principio ISP: expone solo los métodos que el dominio de autoridades requiere.
/// </summary>
public interface IDepartmentAuthorityService : IService<DepartmentAuthority, int>
{
    /// <summary>
    /// Obtiene un resultado paginado de autoridades filtradas por departamento.
    /// </summary>
    /// <param name="departmentId">ID del departamento.</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="onlyActive">Si es true, retorna solo registros activos y vigentes.</param>
    Task<PagedResult<DepartmentAuthority>> GetPagedByDepartmentAsync(
        int departmentId,
        int page,
        int pageSize,
        CancellationToken ct,
        bool onlyActive = false);

    /// <summary>
    /// Obtiene un resultado paginado de autoridades filtradas por empleado.
    /// </summary>
    /// <param name="employeeId">ID del empleado.</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<PagedResult<DepartmentAuthority>> GetPagedByEmployeeAsync(
        int employeeId,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Obtiene todas las autoridades activas de un departamento (sin paginación).
    /// </summary>
    /// <param name="departmentId">ID del departamento.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<List<DepartmentAuthority>> GetActiveByDepartmentAsync(int departmentId, CancellationToken ct);

    /// <summary>
    /// Consulta la denominación de autoridad activa de un empleado por su cédula de identidad.
    /// Realiza el join: People (IdCard) → Employees (PersonID) → DepartmentAuthorities (EmployeeID).
    /// </summary>
    /// <param name="idCard">Número de cédula de identidad.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>DTO con la denominación y datos del empleado, o null si no se encuentra.</returns>
    Task<DepartmentAuthorityDenominationDto?> GetDenominationByIdCardAsync(string idCard, CancellationToken ct);

    /// <summary>
    /// Obtiene un resultado paginado con búsqueda de texto libre.
    /// </summary>
    /// <param name="search">Texto de búsqueda.</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="onlyActive">Si es true, retorna solo registros activos y vigentes.</param>
    Task<PagedResult<DepartmentAuthority>> GetPagedWithSearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct,
        bool onlyActive = false);
}
