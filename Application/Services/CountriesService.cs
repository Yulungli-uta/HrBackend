using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class CountriesService : Service<Countries, string>, ICountriesService
{
    public CountriesService(ICountriesRepository repo) : base(repo) { }
}
