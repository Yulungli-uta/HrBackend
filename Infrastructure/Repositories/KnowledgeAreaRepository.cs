using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class KnowledgeAreaRepository : ServiceAwareEfRepository<KnowledgeArea, int>, IKnowledgeAreaRepository
    {
        private readonly DbContext _db;
        public KnowledgeAreaRepository(WsUtaSystem.Data.AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<KnowledgeArea>> GetByParentAsync(int parentId, CancellationToken ct)
        {
            return await _db.Set<KnowledgeArea>()
                    .Where(ka => ka.ParentId == parentId && ka.IsActive)
                    .ToListAsync(ct);
        }
    }
}
