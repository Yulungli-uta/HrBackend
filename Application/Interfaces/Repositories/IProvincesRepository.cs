using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IProvincesRepository : IRepository<Provinces, string>
{
    Task<IEnumerable<Provinces>> GetByCountryIdAsync(string countryId);
}
