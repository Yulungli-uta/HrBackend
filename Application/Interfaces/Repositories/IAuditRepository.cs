using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Audit;
using WsUtaSystem.Application.DTOs.Common;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IAuditRepository : IRepository<Audit, long>
{
    /// <summary>
    /// Correcciones manuales (Action=CORRECTION) filtradas y paginadas, de la más reciente a la más antigua.
    /// </summary>
    Task<PagedResult<Audit>> SearchCorrectionsAsync(
        string? tableName, string? recordId, string? search, string? userName, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Resuelve número, persona, tipo y estado de los registros auditados indicados. Los que no se
    /// encuentran (o no son visibles por los filtros de datos de prueba) simplemente no vienen en el resultado.
    /// </summary>
    Task<IReadOnlyList<AuditRecordContext>> GetRecordContextsAsync(
        IReadOnlyCollection<int> personnelActionIds, IReadOnlyCollection<int> contractIds, CancellationToken ct);
}
