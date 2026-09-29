using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class BankAccountsService : Service<BankAccounts, int>, IBankAccountsService
{
    private readonly IBankAccountsRepository _repository;
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public BankAccountsService(IBankAccountsRepository repo, WsUtaSystem.Data.AppDbContext db) : base(repo)
    {
        _repository = repo;
        _db = db;
    }

    public async Task<IEnumerable<BankAccounts>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }

    /// <summary>
    /// Hallazgo informe UTA-DITIC-PS-027-2026, observación 44: como máximo una cuenta
    /// "Principal" por persona (no "activa" — la persona puede tener varias cuentas, solo
    /// una marcada principal). Si la cuenta que se crea ya viene marcada como principal,
    /// desmarca las demás de la misma persona en la misma transacción; el índice único
    /// filtrado UX_BankAccounts_PersonPrimary es la garantía real a nivel de BD.
    /// </summary>
    public new async Task<BankAccounts> CreateAsync(BankAccounts entity, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _repository.AddAsync(entity, ct);
            if (entity.IsPrimary)
            {
                await UnsetOtherPrimariesAsync(entity.PersonId, entity.AccountId, ct);
            }
            await tx.CommitAsync(ct);
            return entity;
        });
    }

    /// <inheritdoc cref="CreateAsync"/>
    public new async Task UpdateAsync(int id, BankAccounts entity, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _repository.UpdateAsync(id, entity, ct);
            if (entity.IsPrimary)
            {
                await UnsetOtherPrimariesAsync(entity.PersonId, id, ct);
            }
            await tx.CommitAsync(ct);
        });
    }

    private async Task UnsetOtherPrimariesAsync(int personId, int keepAccountId, CancellationToken ct)
    {
        await _db.Set<BankAccounts>()
            .Where(a => a.PersonId == personId && a.AccountId != keepAccountId && a.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsPrimary, false), ct);
    }
}
