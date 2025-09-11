using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class EmergencyContactsRepository : ServiceAwareEfRepository<EmergencyContacts, int>, IEmergencyContactsRepository
{
    public EmergencyContactsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
