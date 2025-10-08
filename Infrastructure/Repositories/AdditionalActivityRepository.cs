using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class AdditionalActivityRepository : ServiceAwareEfRepository<AdditionalActivity, int>, IAdditionalActivityRepository
{
    public AdditionalActivityRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
