using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class BankAccountsRepository : ServiceAwareEfRepository<BankAccounts, int>, IBankAccountsRepository
{
    public BankAccountsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<BankAccounts>> GetByPersonIdAsync(int personId)
    {
        return await _dbSet.Where(a => a.PersonId == personId).ToListAsync();
    }

}