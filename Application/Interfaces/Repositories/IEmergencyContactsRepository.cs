using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IEmergencyContactsRepository : IRepository<EmergencyContacts, int>
{
    Task<IEnumerable<EmergencyContacts>> GetByPersonIdAsync(int personId);
}
