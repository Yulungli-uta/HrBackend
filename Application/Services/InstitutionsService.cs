using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class InstitutionsService : Service<Institutions, int>, IInstitutionsService
{
    public InstitutionsService(IInstitutionsRepository repo) : base(repo) { }
}
