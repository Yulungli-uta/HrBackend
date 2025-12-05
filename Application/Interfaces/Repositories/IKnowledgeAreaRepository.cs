using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IKnowledgeAreaRepository : IRepository<KnowledgeArea, int>{
        Task<IEnumerable<KnowledgeArea>> GetByParentAsync(int parentId, CancellationToken ct);
   
    }
}
