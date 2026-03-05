using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class DocflowMovementRepository : IDocflowMovementRepository
    {
        private readonly AppDbContext _db;
        public DocflowMovementRepository(AppDbContext db) => _db = db;

        public async Task CreateAsync(DocflowWorkflowMovement movement, CancellationToken ct)
        {
            _db.DocflowMovements.Add(movement);
            await _db.SaveChangesAsync(ct);
        }

        public Task<DocflowWorkflowMovement?> GetLastMovementAsync(Guid instanceId, CancellationToken ct) =>
            _db.DocflowMovements.AsNoTracking()
                .Where(m => m.InstanceId == instanceId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(ct);

        public async Task<List<DocflowWorkflowMovement>> GetReturnsAuditAsync(DateTime? from, DateTime? to, int? processId, int? userId, CancellationToken ct)
        {
            var q = _db.DocflowMovements.AsNoTracking()
                .Where(m => m.MovementType == "RETURN");

            if (from.HasValue) q = q.Where(m => m.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(m => m.CreatedAt <= to.Value);
            if (processId.HasValue) q = q.Where(m => m.FromProcessId == processId.Value || m.ToProcessId == processId.Value);
            if (userId.HasValue) q = q.Where(m => m.CreatedBy == userId.Value);

            return await q.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
        }
    }
}