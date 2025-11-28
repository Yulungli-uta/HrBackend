using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmergencyContactsRepository : ServiceAwareEfRepository<EmergencyContacts, int>, IEmergencyContactsRepository
{
    public EmergencyContactsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<EmergencyContacts>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(e => e.PersonId == personId).ToListAsync();
    }

}