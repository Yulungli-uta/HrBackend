using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class ContractTypeRepository : ServiceAwareEfRepository<ContractType, int>, IContractTypeRepository
{
    public ContractTypeRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<ContractType?> GetWithDefaultTemplateAsync(int contractTypeId, CancellationToken ct = default)
        => await _db.Set<ContractType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct);

    public async Task SetDefaultTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default)
    {
        var entity = await _db.Set<ContractType>()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct)
            ?? throw new KeyNotFoundException($"ContractType id={contractTypeId} no existe.");

        entity.DefaultTemplateId = templateId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int contractTypeId,
        int year,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var entity = await _db.Set<ContractType>()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct)
            ?? throw new KeyNotFoundException($"ContractType id={contractTypeId} no existe.");

        if (string.IsNullOrWhiteSpace(entity.NumberingPrefix))
            throw new InvalidOperationException(
                $"El tipo de contrato id={contractTypeId} no tiene un prefijo de numeración configurado.");

        if (entity.NumberingYear != year)
        {
            entity.NumberingYear = year;
            entity.NumberingLastSequence = 0;
        }

        entity.NumberingLastSequence++;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var docNumber = $"{entity.NumberingPrefix}-{year}-{entity.NumberingLastSequence:D3}";
        return (docNumber, year, entity.NumberingLastSequence);
    }
}
