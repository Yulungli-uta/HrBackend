using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class BankAccountsRepository : ServiceAwareEfRepository<BankAccounts, int>, IBankAccountsRepository
{
    private readonly DbContext _db;
    public BankAccountsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<BankAccounts>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<BankAccounts>().Where(a => a.PersonId == personId).ToListAsync();
    }

}