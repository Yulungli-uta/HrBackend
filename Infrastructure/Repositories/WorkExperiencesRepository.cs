using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class WorkExperiencesRepository : ServiceAwareEfRepository<WorkExperiences, int>, IWorkExperiencesRepository
{
    public WorkExperiencesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<WorkExperiences>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(w => w.PersonId == personId).ToListAsync();
    }
}
