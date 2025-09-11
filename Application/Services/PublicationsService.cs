using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PublicationsService : Service<Publications, int>, IPublicationsService
{
    public PublicationsService(IPublicationsRepository repo) : base(repo) { }
}
