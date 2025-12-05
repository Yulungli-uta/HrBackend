using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmergencyContactsRepository : ServiceAwareEfRepository<EmergencyContacts, int>, IEmergencyContactsRepository
{
    private readonly DbContext _db;
    public EmergencyContactsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { 
        _db = db;
    }

    public async Task<IEnumerable<EmergencyContacts>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<EmergencyContacts>().Where(e => e.PersonId == personId).ToListAsync();
    }

}