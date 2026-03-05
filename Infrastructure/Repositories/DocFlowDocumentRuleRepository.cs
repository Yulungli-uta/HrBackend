using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Controllers.Docflow;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class DocFlowDocumentRuleRepository : ServiceAwareEfRepository<DocflowDocumentRule, int>, IDocFlowDocumentRuleRepository
    {
        private readonly AppDbContext _db;

        public DocFlowDocumentRuleRepository(AppDbContext db) : base(db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

    }
}
