using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PublicationsRepository : ServiceAwareEfRepository<Publications, int>, IPublicationsRepository
{
    public PublicationsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
