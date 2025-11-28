using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EmergencyContactsService : Service<EmergencyContacts, int>, IEmergencyContactsService
{
    private readonly IEmergencyContactsRepository _repository;

    public EmergencyContactsService(IEmergencyContactsRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<EmergencyContacts>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
