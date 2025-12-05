using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class TrainingsRepository : ServiceAwareEfRepository<Trainings, int>, ITrainingsRepository
{
    private readonly DbContext _db;
    public TrainingsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<Trainings>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<Trainings>().Where(t => t.PersonId == personId).ToListAsync();
    }
}
