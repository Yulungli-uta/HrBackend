using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IKnowledgeAreaService : IService<KnowledgeArea, int>
    {
        Task<IEnumerable<KnowledgeArea>> GetByParentAsync(int parentId, CancellationToken ct);
    
    }
}
