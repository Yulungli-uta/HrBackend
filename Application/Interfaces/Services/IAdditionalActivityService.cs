using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IAdditionalActivityService : IService<AdditionalActivity, int> 
{
    Task<IEnumerable<AdditionalActivity>> GetByContractIdAsync(int ContractId, CancellationToken ct);
}
