using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class BankAccountsRepository : ServiceAwareEfRepository<BankAccounts, int>, IBankAccountsRepository
{
    public BankAccountsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
