using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Audit;
using WsUtaSystem.Application.DTOs.Common;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IAuditService : IService<Audit, long>
{
    /// <summary>
    /// Correcciones manuales (Action=CORRECTION) de HR.Audit, paginadas y enriquecidas con el número,
    /// la persona y el tipo del registro corregido (acción de personal o contrato).
    /// </summary>
    Task<PagedResult<AuditCorrectionDto>> SearchCorrectionsAsync(
        string? tableName, string? recordId, string? search, string? userName, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct);
}
