using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class JobActivityRepository : ServiceAwareEfRepository<JobActivity, int>, IJobActivityRepository
{
    public JobActivityRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
