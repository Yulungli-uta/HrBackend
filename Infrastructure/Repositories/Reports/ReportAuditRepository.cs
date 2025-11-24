using Dapper;
using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Infrastructure.Repositories.Reports;

/// <summary>
/// Repositorio para auditoría de reportes
/// </summary>
public class ReportAuditRepository
{
    private readonly string _connectionString;

    public ReportAuditRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<int> CreateAuditAsync(CreateReportAuditDto audit)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var result = await connection.QuerySingleAsync<int>(
            "[HR].[sp_InsertReportAudit]",
            new
            {
                audit.UserId,
                audit.UserEmail,
                audit.ReportType,
                audit.ReportFormat,
                audit.FiltersApplied,
                audit.FileSizeBytes,
                audit.GenerationTimeMs,
                audit.ClientIp,
                audit.Success,
                audit.ErrorMessage,
                audit.FileName
            },
            commandType: System.Data.CommandType.StoredProcedure
        );

        return result;
    }

    public async Task<IEnumerable<ReportAuditDto>> GetAuditsAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? reportType,
        Guid? userId,
        int top)
    {
        using var connection = new SqlConnection(_connectionString);
        
        return await connection.QueryAsync<ReportAuditDto>(
            "[HR].[sp_GetReportAudits]",
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                ReportType = reportType,
                UserId = userId,
                Top = top
            },
            commandType: System.Data.CommandType.StoredProcedure
        );
    }
}
