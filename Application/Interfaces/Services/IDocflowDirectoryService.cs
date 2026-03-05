using WsUtaSystem.Application.DTOs.Docflow;

namespace WsUtaSystem.Application.Interfaces.Services
{
    /// <summary>
    /// Servicio especializado para obtener catálogos y directorios en Docflow.
    /// Reutiliza servicios existentes (RefTypes, Departments, Employees) para poblar catálogos.
    /// </summary>
    public interface IDocflowDirectoryService
    {
        /// <summary>
        /// Obtiene los estados posibles de un expediente.
        /// </summary>
        Task<IReadOnlyList<DirectoryParameterDto>> GetInstanceStatusesAsync(CancellationToken ct);

        /// <summary>
        /// Obtiene los tipos de movimiento disponibles.
        /// </summary>
        Task<IReadOnlyList<DirectoryParameterDto>> GetMovementTypesAsync(CancellationToken ct);

        /// <summary>
        /// Obtiene los niveles de prioridad disponibles.
        /// </summary>
        Task<IReadOnlyList<DirectoryParameterDto>> GetPrioritiesAsync(CancellationToken ct);

        /// <summary>
        /// Obtiene las áreas/departamentos disponibles con su jerarquía.
        /// </summary>
        Task<IReadOnlyList<DirectoryParameterDto>> GetAreasAsync(CancellationToken ct);

        /// <summary>
        /// Obtiene los usuarios (empleados) disponibles en el sistema.
        /// </summary>
        Task<IReadOnlyList<DirectoryParameterDto>> GetUsersAsync(CancellationToken ct);

        /// <summary>
        /// Obtiene un catálogo genérico por tipo.
        /// </summary>
        Task<IReadOnlyList<DirectoryParameterDto>> GetDirectoryByTypeAsync(string type, CancellationToken ct);
    }
}
