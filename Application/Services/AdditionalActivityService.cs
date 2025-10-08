using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AdditionalActivityService : Service<AdditionalActivity, int>, IAdditionalActivityService
{
    public AdditionalActivityService(IAdditionalActivityRepository repo) : base(repo) { }
}
