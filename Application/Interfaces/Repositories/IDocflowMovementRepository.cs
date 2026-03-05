using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IDocflowMovementRepository
    {
        Task CreateAsync(DocflowWorkflowMovement movement, CancellationToken ct);
        Task<DocflowWorkflowMovement?> GetLastMovementAsync(Guid instanceId, CancellationToken ct);
        Task<List<DocflowWorkflowMovement>> GetReturnsAuditAsync(DateTime? from, DateTime? to, int? processId, int? userId, CancellationToken ct);
    }
}
