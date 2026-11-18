using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Audit;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class AuditRepository : ServiceAwareEfRepository<Audit, long>, IAuditRepository
{
    private const string CorrectionAction = "CORRECTION";
    private const string PersonnelActionsTable = "PersonnelActions";
    private const string ContractsTable = "Contracts";
    /// <summary>La base distingue tildes (SQL_Latin1_General_CP1_CI_AS): para buscar nombres, "MUÑOZ" debe coincidir con "munoz".</summary>
    private const string AccentInsensitive = "Latin1_General_CI_AI";

    public AuditRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    /// <inheritdoc />
    public async Task<PagedResult<Audit>> SearchCorrectionsAsync(
        string? tableName, string? recordId, string? search, string? userName, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Audit.AsNoTracking().Where(a => a.Action == CorrectionAction);

        if (!string.IsNullOrWhiteSpace(tableName)) query = query.Where(a => a.TableName == tableName);
        if (!string.IsNullOrWhiteSpace(recordId)) query = query.Where(a => a.RecordId == recordId);
        if (!string.IsNullOrWhiteSpace(search)) query = ApplyRecordSearch(query, search.Trim());
        if (!string.IsNullOrWhiteSpace(userName)) query = query.Where(a => a.UserName.Contains(userName));
        if (dateFrom.HasValue) query = query.Where(a => a.DateTime >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(a => a.DateTime <= dateTo.Value);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.DateTime)
            .ThenByDescending(a => a.AuditId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Audit> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    /// <summary>
    /// Filtra las correcciones por el registro corregido: número de acción, código de contrato, cédula o nombre de la
    /// persona (cada palabra escrita debe coincidir), o el ID interno exacto si se escribe solo un número.
    /// </summary>
    private IQueryable<Audit> ApplyRecordSearch(IQueryable<Audit> query, string search)
    {
        var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var actions = _db.PersonnelActions.AsNoTracking().AsQueryable();
        var contracts = _db.Contracts.AsNoTracking().AsQueryable();
        foreach (var term in terms)
        {
            var t = term;
            actions = actions.Where(a =>
                (a.ActionNumber != null && a.ActionNumber.Contains(t))
                || _db.People.Any(p => p.PersonId == a.PersonId
                    && (p.IdCard.Contains(t)
                        || EF.Functions.Collate(p.LastName, AccentInsensitive).Contains(t)
                        || EF.Functions.Collate(p.FirstName, AccentInsensitive).Contains(t))));
            contracts = contracts.Where(c =>
                c.ContractCode.Contains(t)
                || _db.People.Any(p => p.PersonId == c.PersonID
                    && (p.IdCard.Contains(t)
                        || EF.Functions.Collate(p.LastName, AccentInsensitive).Contains(t)
                        || EF.Functions.Collate(p.FirstName, AccentInsensitive).Contains(t))));
        }

        var actionIds = actions.Select(a => a.ActionId.ToString());
        var contractIds = contracts.Select(c => c.ContractID.ToString());

        return query.Where(a =>
            a.RecordId == search
            || (a.TableName == PersonnelActionsTable && actionIds.Contains(a.RecordId))
            || (a.TableName == ContractsTable && contractIds.Contains(a.RecordId)));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditRecordContext>> GetRecordContextsAsync(
        IReadOnlyCollection<int> personnelActionIds, IReadOnlyCollection<int> contractIds, CancellationToken ct)
    {
        var result = new List<AuditRecordContext>();

        if (personnelActionIds.Count > 0)
        {
            var actions = await (
                from a in _db.PersonnelActions.AsNoTracking()
                where personnelActionIds.Contains(a.ActionId)
                join p in _db.People.AsNoTracking() on a.PersonId equals p.PersonId into people
                from p in people.DefaultIfEmpty()
                join t in _db.PersonnelActionTypes.AsNoTracking() on a.ActionTypeId equals t.PersonnelActionTypeId into types
                from t in types.DefaultIfEmpty()
                select new
                {
                    a.ActionId,
                    a.ActionNumber,
                    PersonName = p == null ? null : p.LastName + " " + p.FirstName,
                    TypeName = t == null ? null : t.Name,
                    a.Status
                }).ToListAsync(ct);

            result.AddRange(actions.Select(x => new AuditRecordContext(
                PersonnelActionsTable, x.ActionId.ToString(), x.ActionNumber, x.PersonName, x.TypeName, x.Status)));
        }

        if (contractIds.Count > 0)
        {
            var contracts = await (
                from c in _db.Contracts.AsNoTracking()
                where contractIds.Contains(c.ContractID)
                join p in _db.People.AsNoTracking() on c.PersonID equals p.PersonId into people
                from p in people.DefaultIfEmpty()
                select new
                {
                    c.ContractID,
                    c.ContractCode,
                    PersonName = p == null ? null : p.LastName + " " + p.FirstName
                }).ToListAsync(ct);

            result.AddRange(contracts.Select(x => new AuditRecordContext(
                ContractsTable, x.ContractID.ToString(), x.ContractCode, x.PersonName, null, null)));
        }

        return result;
    }
}
