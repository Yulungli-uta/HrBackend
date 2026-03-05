using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class DocflowInstanceRepository : IDocflowInstanceRepository
    {
        private readonly AppDbContext _db;
        public DocflowInstanceRepository(AppDbContext db) => _db = db;

        public Task<DocflowWorkflowInstance?> GetByIdAsync(Guid instanceId, CancellationToken ct) =>
            _db.DocflowInstances.FirstOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        public Task<bool> ExistsAsync(Guid instanceId, CancellationToken ct) =>
            _db.DocflowInstances.AsNoTracking().AnyAsync(x => x.InstanceId == instanceId, ct);

        public async Task<(List<DocflowWorkflowInstance> Items, long Total)> SearchAsync(
            int page, int pageSize, string? status, int? processId, string? q, DateTime? from, DateTime? to,
            int currentUserDepartmentId,
            CancellationToken ct)
        {
            // Acceso lectura: depto actual OR depto participó
            // Para paginación eficiente, primero filtra por depto actual (principal) y luego amplia por participación.
            // En ambientes grandes, lo correcto es materializar una tabla “InstanceAccess” por depto, pero por default lo hacemos on-the-fly.

            var baseQuery = _db.DocflowInstances.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
                baseQuery = baseQuery.Where(x => x.CurrentStatus == status);

            if (processId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProcessId == processId.Value);

            if (from.HasValue)
                baseQuery = baseQuery.Where(x => x.CreatedAt >= from.Value);

            if (to.HasValue)
                baseQuery = baseQuery.Where(x => x.CreatedAt <= to.Value);

            // q: por default busca en JSON metadata (si usas JSON). Es costoso; lo dejo como contains simple.
            if (!string.IsNullOrWhiteSpace(q))
                baseQuery = baseQuery.Where(x => x.DynamicMetadata != null && x.DynamicMetadata.Contains(q));

            // Acceso lectura: depto actual o participó
            // Participación se evalúa contra movimientos.
            var readableQuery =
                baseQuery.Where(i => i.CurrentDepartmentId == currentUserDepartmentId)
                .Union(
                    baseQuery.Where(i =>
                        _db.DocflowMovements.AsNoTracking()
                            .Any(m => m.InstanceId == i.InstanceId &&
                                     (m.FromDepartmentId == currentUserDepartmentId || m.ToDepartmentId == currentUserDepartmentId))
                    )
                );

            var total = await readableQuery.LongCountAsync(ct);

            var items = await readableQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task CreateAsync(DocflowWorkflowInstance instance, CancellationToken ct)
        {
            _db.DocflowInstances.Add(instance);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(DocflowWorkflowInstance instance, CancellationToken ct)
        {
            _db.DocflowInstances.Update(instance);
            await _db.SaveChangesAsync(ct);
        }

        public Task<bool> HasDepartmentParticipatedAsync(Guid instanceId, int departmentId, CancellationToken ct) =>
            _db.DocflowMovements.AsNoTracking()
                .AnyAsync(m => m.InstanceId == instanceId && (m.FromDepartmentId == departmentId || m.ToDepartmentId == departmentId), ct);

        /// <summary>
        /// Verifica si existen instancias activas para un proceso específico.
        /// </summary>
        public async Task<bool> HasActiveInstancesAsync(int processId, CancellationToken ct)
        {
            return await _db.DocflowInstances
                .AsNoTracking()
                .AnyAsync(x => x.ProcessId == processId && x.CurrentStatus != "COMPLETED" && x.CurrentStatus != "CANCELLED", ct);
        }
    }

}
