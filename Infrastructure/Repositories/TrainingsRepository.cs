using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class TrainingsRepository : ServiceAwareEfRepository<Trainings, int>, ITrainingsRepository
{
    public TrainingsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<Trainings>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(t => t.PersonId == personId).ToListAsync();
    }
}
