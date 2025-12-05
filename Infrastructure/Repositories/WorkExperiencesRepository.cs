using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class WorkExperiencesRepository : ServiceAwareEfRepository<WorkExperiences, int>, IWorkExperiencesRepository
{
    private readonly DbContext _db;
    public WorkExperiencesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<WorkExperiences>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<WorkExperiences>().Where(w => w.PersonId == personId).ToListAsync();
    }
}
