using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EducationLevelsRepository : ServiceAwareEfRepository<EducationLevels, int>, IEducationLevelsRepository
{
    public EducationLevelsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(e => e.PersonId == personId).ToListAsync();
    }

}