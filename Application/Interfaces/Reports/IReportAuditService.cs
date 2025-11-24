using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Reports;

/// <summary>
/// Servicio de auditoría de reportes
/// </summary>
public interface IReportAuditService
{
    Task<int> CreateAuditAsync(CreateReportAuditDto audit);
    Task<IEnumerable<ReportAuditDto>> GetAuditsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? reportType = null,
        Guid? userId = null,
        int top = 100);
}
