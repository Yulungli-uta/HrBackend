using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class TrainingsRepository : ServiceAwareEfRepository<Trainings, int>, ITrainingsRepository
{
    public TrainingsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
