using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EmergencyContactsService : Service<EmergencyContacts, int>, IEmergencyContactsService
{
    public EmergencyContactsService(IEmergencyContactsRepository repo) : base(repo) { }
}
