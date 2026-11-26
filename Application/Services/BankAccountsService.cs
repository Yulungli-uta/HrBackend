using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
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
    public override async Task<BankAccounts> CreateAsync(BankAccounts entity, CancellationToken ct)
    {
        await EnsureNotDuplicateAsync(entity, excludeAccountId: 0, ct);
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
    public override async Task UpdateAsync(int id, BankAccounts entity, CancellationToken ct)
    {
        // Solo se revisa si cambia la entidad, el número o la persona: así una cuenta que ya estaba repetida de antes puede
        // seguir actualizándose (por ejemplo para marcarla como principal) sin que el duplicado anterior lo impida.
        var current = await _repository.GetByIdAsync(id, ct);
        var changed = current is null
            || current.PersonId != entity.PersonId
            || !BankAccountRules.IsSameAccount(current.FinancialInstitution, current.AccountNumber, entity.FinancialInstitution, entity.AccountNumber);
        if (changed) await EnsureNotDuplicateAsync(entity, id, ct);

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

    /// <summary>
    /// Control de duplicidad: la misma entidad y el mismo número no pueden registrarse dos veces para la misma persona ni
    /// estar ya registrados para otra persona (un número de cuenta pertenece a una sola cuenta de una entidad). El mensaje
    /// de otra persona no revela de quién es. Lanza <see cref="BusinessRuleException"/> con un mensaje para el usuario.
    /// </summary>
    private async Task EnsureNotDuplicateAsync(BankAccounts entity, int excludeAccountId, CancellationToken ct)
    {
        if (BankAccountRules.NormalizeNumber(entity.AccountNumber).Length == 0) return;

        var ofSamePerson = await _db.Set<BankAccounts>().AsNoTracking()
            .Where(a => a.PersonId == entity.PersonId && a.AccountId != excludeAccountId)
            .Select(a => new { a.FinancialInstitution, a.AccountNumber })
            .ToListAsync(ct);
        if (ofSamePerson.Any(a => BankAccountRules.IsSameAccount(a.FinancialInstitution, a.AccountNumber, entity.FinancialInstitution, entity.AccountNumber)))
            throw new BusinessRuleException(
                $"La cuenta {entity.AccountNumber.Trim()} de {entity.FinancialInstitution.Trim()} ya está registrada para esta persona.");

        var number = entity.AccountNumber.Trim();
        var institutionsOfOthers = await _db.Set<BankAccounts>().AsNoTracking()
            .Where(a => a.PersonId != entity.PersonId && a.AccountNumber == number)
            .Select(a => a.FinancialInstitution)
            .ToListAsync(ct);
        var wanted = BankAccountRules.NormalizeInstitution(entity.FinancialInstitution);
        if (institutionsOfOthers.Any(i => BankAccountRules.NormalizeInstitution(i) == wanted))
            throw new BusinessRuleException(
                "Ese número de cuenta ya está registrado en esa entidad financiera para otra persona. " +
                "Verifique el número; si es correcto, comuníquese con Talento Humano.");
    }

    private async Task UnsetOtherPrimariesAsync(int personId, int keepAccountId, CancellationToken ct)
    {
        await _db.Set<BankAccounts>()
            .Where(a => a.PersonId == personId && a.AccountId != keepAccountId && a.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsPrimary, false), ct);
    }
}
