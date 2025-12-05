using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class KnowledgeAreaService : Service<KnowledgeArea, int>, IKnowledgeAreaService
    {
        private readonly IKnowledgeAreaRepository _repository;
        public KnowledgeAreaService(IKnowledgeAreaRepository repo) : base(repo)
        {

            _repository = repo;
        }

        public async Task<IEnumerable<KnowledgeArea>> GetByParentAsync(int parentId, CancellationToken ct)
        {
            return await _repository.GetByParentAsync(parentId, ct);
        }
    }
}
