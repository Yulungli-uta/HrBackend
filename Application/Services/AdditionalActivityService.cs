using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AdditionalActivityService : Service<AdditionalActivity, int>, IAdditionalActivityService
{
    private readonly IAdditionalActivityRepository   _repository;
    private readonly ILogger<AdditionalActivityService> _logger;
    public AdditionalActivityService(IAdditionalActivityRepository repo) : base(repo) { 
        _repository = repo;
    }

    public async Task<IEnumerable<AdditionalActivity>> GetByContractIdAsync(int ContractId, CancellationToken ct)
    {
        return await _repository.GetByContractIDAsync(ContractId, ct);
    }
}
