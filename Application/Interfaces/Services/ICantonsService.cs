using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface ICantonsService : IService<Cantons, string>
{
    Task<IEnumerable<Cantons>> GetByProvinceIdAsync(string provinceId);
}
