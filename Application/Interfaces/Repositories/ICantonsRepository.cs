using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface ICantonsRepository : IRepository<Cantons, string>
{
    Task<IEnumerable<Cantons>> GetByProvinceIdAsync(string provinceId);
}
