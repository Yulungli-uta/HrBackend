using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class BankAccountsService : Service<BankAccounts, int>, IBankAccountsService
{
    private readonly IBankAccountsRepository _repository;

    public BankAccountsService(IBankAccountsRepository repo) : base(repo)
    {
        _repository = repo;
    }

    public async Task<IEnumerable<BankAccounts>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }
}
