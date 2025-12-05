using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EducationLevelsRepository : ServiceAwareEfRepository<EducationLevels, int>, IEducationLevelsRepository
{

    private readonly DbContext _db;
    public EducationLevelsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<EducationLevels>().Where(e => e.PersonId == personId).ToListAsync();
    }

}