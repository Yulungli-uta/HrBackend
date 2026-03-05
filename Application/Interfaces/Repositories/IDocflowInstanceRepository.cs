using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IDocflowInstanceRepository
    {
        Task<DocflowWorkflowInstance?> GetByIdAsync(Guid instanceId, CancellationToken ct);
        Task<bool> ExistsAsync(Guid instanceId, CancellationToken ct);
        Task<(List<DocflowWorkflowInstance> Items, long Total)> SearchAsync(
            int page, int pageSize, string? status, int? processId, string? q, DateTime? from, DateTime? to,
            int currentUserDepartmentId,
            CancellationToken ct);

        Task CreateAsync(DocflowWorkflowInstance instance, CancellationToken ct);
        Task UpdateAsync(DocflowWorkflowInstance instance, CancellationToken ct);

        Task<bool> HasDepartmentParticipatedAsync(Guid instanceId, int departmentId, CancellationToken ct);

        /// <summary>
        /// Verifica si existen instancias activas para un proceso específico.
        /// </summary>
        Task<bool> HasActiveInstancesAsync(int processId, CancellationToken ct);
    }
}
