using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Audit;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AuditService : Service<Audit, long>, IAuditService
{
    private readonly IAuditRepository _auditRepo;

    public AuditService(IAuditRepository repo) : base(repo) { _auditRepo = repo; }

    /// <inheritdoc />
    public async Task<PagedResult<AuditCorrectionDto>> SearchCorrectionsAsync(
        string? tableName, string? recordId, string? search, string? userName, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct)
    {
        var paged = await _auditRepo.SearchCorrectionsAsync(tableName, recordId, search, userName, dateFrom, dateTo, page, pageSize, ct);

        var actionIds = ParseIds(paged.Items, "PersonnelActions");
        var contractIds = ParseIds(paged.Items, "Contracts");
        var contexts = (await _auditRepo.GetRecordContextsAsync(actionIds, contractIds, ct))
            .ToDictionary(c => (c.TableName, c.RecordId));

        var items = paged.Items.Select(a =>
        {
            contexts.TryGetValue((a.TableName, a.RecordId), out var ctx);
            return new AuditCorrectionDto
            {
                AuditId = a.AuditId,
                TableName = a.TableName,
                Action = a.Action,
                RecordId = a.RecordId,
                UserName = a.UserName,
                DateTime = a.DateTime,
                Details = a.Details!,
                RecordNumber = ctx?.RecordNumber,
                PersonName = ctx?.PersonName,
                RecordTypeName = ctx?.RecordTypeName,
                RecordStatus = ctx?.RecordStatus
            };
        }).ToList();

        return new PagedResult<AuditCorrectionDto>
        {
            Items = items, Page = paged.Page, PageSize = paged.PageSize, TotalCount = paged.TotalCount
        };
    }

    private static List<int> ParseIds(IEnumerable<Audit> rows, string table) =>
        rows.Where(a => a.TableName == table && int.TryParse(a.RecordId, out _))
            .Select(a => int.Parse(a.RecordId))
            .Distinct()
            .ToList();
}
