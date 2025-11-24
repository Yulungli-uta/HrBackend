using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;
using WsUtaSystem.Infrastructure.Repositories.Reports;

namespace WsUtaSystem.Application.Services.Reports;

/// <summary>
/// Servicio de auditoría de reportes
/// </summary>
public class ReportAuditService : IReportAuditService
{
    private readonly ReportAuditRepository _repository;
    private readonly ILogger<ReportAuditService> _logger;

    public ReportAuditService(
        ReportAuditRepository repository,
        ILogger<ReportAuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> CreateAuditAsync(CreateReportAuditDto audit)
    {
        try
        {
            var auditId = await _repository.CreateAuditAsync(audit);
            
            _logger.LogInformation(
                "Report audit created: {ReportType} {ReportFormat} by {UserEmail}",
                audit.ReportType,
                audit.ReportFormat,
                audit.UserEmail
            );

            return auditId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report audit for {ReportType}", audit.ReportType);
            throw;
        }
    }

    public async Task<IEnumerable<ReportAuditDto>> GetAuditsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? reportType = null,
        Guid? userId = null,
        int top = 100)
    {
        try
        {
            return await _repository.GetAuditsAsync(startDate, endDate, reportType, userId, top);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report audits");
            throw;
        }
    }
}
