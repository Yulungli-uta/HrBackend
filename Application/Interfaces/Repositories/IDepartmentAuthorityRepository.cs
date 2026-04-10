using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>
/// Contrato de repositorio para la entidad <see cref="DepartmentAuthority"/>.
/// Extiende el repositorio genérico con consultas especializadas del dominio.
/// Principio ISP: solo expone los métodos que el dominio de autoridades requiere.
/// </summary>
public interface IDepartmentAuthorityRepository : IRepository<DepartmentAuthority, int>
{
    /// <summary>
    /// Obtiene un resultado paginado de autoridades filtradas por departamento,
    /// incluyendo las entidades relacionadas (Employee → People, AuthorityType, Job).
    /// </summary>
    /// <param name="departmentId">ID del departamento a filtrar.</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="onlyActive">Si es true, retorna solo los registros con IsActive = true y EndDate IS NULL.</param>
    Task<PagedResult<DepartmentAuthority>> GetPagedByDepartmentAsync(
        int departmentId,
        int page,
        int pageSize,
        CancellationToken ct,
        bool onlyActive = false);

    /// <summary>
    /// Obtiene un resultado paginado de autoridades filtradas por empleado,
    /// incluyendo las entidades relacionadas.
    /// </summary>
    /// <param name="employeeId">ID del empleado a filtrar.</param>
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
    /// Usar únicamente para catálogos pequeños o exportaciones.
    /// </summary>
    /// <param name="departmentId">ID del departamento.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<List<DepartmentAuthority>> GetActiveByDepartmentAsync(int departmentId, CancellationToken ct);

    /// <summary>
    /// Obtiene la denominación activa de un empleado identificado por su cédula de identidad.
    /// Realiza el join: People (IdCard) → Employees (PersonID) → DepartmentAuthorities (EmployeeID).
    /// Retorna null si no se encuentra el empleado o no tiene autoridad activa.
    /// </summary>
    /// <param name="idCard">Número de cédula de identidad de la persona.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<DepartmentAuthority?> GetActiveAuthorityByIdCardAsync(string idCard, CancellationToken ct);

    /// <summary>
    /// Obtiene un resultado paginado con filtro de búsqueda por texto libre.
    /// Busca en: Denomination, ResolutionCode, Notes, nombre del departamento, nombre del empleado.
    /// </summary>
    /// <param name="search">Texto de búsqueda (puede ser null o vacío para sin filtro).</param>
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
