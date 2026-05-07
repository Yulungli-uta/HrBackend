using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class PersonnelActionTypeRepository
    : ServiceAwareEfRepository<PersonnelActionType, int>, IPersonnelActionTypeRepository
{
    public PersonnelActionTypeRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<List<PersonnelActionType>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public async Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int personnelActionTypeId,
        int year,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var entity = await _db.Set<PersonnelActionType>()
            .FirstOrDefaultAsync(x => x.PersonnelActionTypeId == personnelActionTypeId, ct)
            ?? throw new KeyNotFoundException(
                $"PersonnelActionType id={personnelActionTypeId} no existe.");

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
