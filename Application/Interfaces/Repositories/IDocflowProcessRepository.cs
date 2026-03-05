using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio especializado para operaciones de procesos en Docflow.
    /// Hereda operaciones CRUD básicas de IRepository y agrega operaciones específicas.
    /// </summary>
    public interface IDocflowProcessRepository : IRepository<DocflowProcessHierarchy, int>
    {
        /// <summary>
        /// Obtiene todos los procesos activos ordenados por ProcessId.
        /// </summary>
        Task<List<DocflowProcessHierarchy>> GetAllActiveAsync(CancellationToken ct);

        /// <summary>
        /// Obtiene las reglas de documento asociadas a un proceso específico.
        /// </summary>
        Task<List<DocflowDocumentRule>> GetRulesByProcessAsync(int processId, CancellationToken ct);

        /// <summary>
        /// Obtiene la transición por defecto desde un proceso.
        /// </summary>
        Task<DocflowProcessTransition?> GetDefaultTransitionAsync(int fromProcessId, CancellationToken ct);

        /// <summary>
        /// Obtiene la metadata de campos dinámicos de un proceso en formato JSON.
        /// </summary>
        Task<string?> GetDynamicFieldMetadataJsonAsync(int processId, CancellationToken ct);

        /// <summary>
        /// Actualiza la metadata de campos dinámicos de un proceso.
        /// </summary>
        Task UpdateDynamicFieldMetadataJsonAsync(int processId, string? json, CancellationToken ct);

        /// <summary>
        /// Obtiene procesos por departamento responsable.
        /// </summary>
        Task<List<DocflowProcessHierarchy>> GetByDepartmentAsync(int departmentId, CancellationToken ct);

        /// <summary>
        /// Obtiene procesos hijo de un proceso padre (sub-procesos).
        /// </summary>
        Task<List<DocflowProcessHierarchy>> GetChildrenAsync(int parentProcessId, CancellationToken ct);

        /// <summary>
        /// Crea una nueva regla de documento para un proceso.
        /// </summary>
        Task<DocflowDocumentRule> CreateRuleAsync(DocflowDocumentRule rule, CancellationToken ct);

        /// <summary>
        /// Actualiza una regla de documento existente.
        /// </summary>
        Task UpdateRuleAsync(int ruleId, DocflowDocumentRule rule, CancellationToken ct);

        /// <summary>
        /// Elimina una regla de documento.
        /// </summary>
        Task DeleteRuleAsync(int ruleId, CancellationToken ct);

        /// <summary>
        /// Obtiene una regla específica por su ID.
        /// </summary>
        Task<DocflowDocumentRule?> GetRuleByIdAsync(int ruleId, CancellationToken ct);
    }
}
