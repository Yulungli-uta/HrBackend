using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IAdditionalActivityRepository : IRepository<AdditionalActivity, int> 
{
    Task<IEnumerable<AdditionalActivity>> GetByContractIDAsync(int ContractId, CancellationToken ct);
}
