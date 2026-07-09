using Dapper;
using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories.Reports;

/// <summary>
/// Repositorio para auditoría de reportes
/// </summary>
public class ReportAuditRepository
{
    private readonly string _connectionString;
    private readonly IRepository<ReportAudit, int> _reportAuditRepository;

    public ReportAuditRepository(
        IConfiguration configuration,
        IRepository<ReportAudit, int> reportAuditRepository)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConn")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings' not found.");
        _reportAuditRepository = reportAuditRepository;
    }

    public async Task<int> CreateAuditAsync(CreateReportAuditDto audit)
    {
        // ── Reemplazado: dejaba de usarse [HR].[sp_InsertReportAudit] (respaldado en
        // Database/hr/99_legacy_sp_backup_20260701.sql) a favor del repositorio genérico EF Core.
        // using var connection = new SqlConnection(_connectionString);
        //
        // var result = await connection.QuerySingleAsync<int>(
        //     "[HR].[sp_InsertReportAudit]",
        //     new
        //     {
        //         audit.UserId,
        //         audit.UserEmail,
        //         audit.ReportType,
        //         audit.ReportFormat,
        //         audit.FiltersApplied,
        //         audit.FileSizeBytes,
        //         audit.GenerationTimeMs,
        //         audit.ClientIp,
        //         audit.Success,
        //         audit.ErrorMessage,
        //         audit.FileName
        //     },
        //     commandType: System.Data.CommandType.StoredProcedure
        // );
        //
        // return result;

        var entity = new ReportAudit
        {
            UserId = audit.UserId ?? throw new InvalidOperationException(
                "UserId es requerido para registrar la auditoría del reporte."),
            UserEmail = audit.UserEmail,
            ReportType = audit.ReportType,
            ReportFormat = audit.ReportFormat,
            FiltersApplied = audit.FiltersApplied,
            GeneratedAt = DateTime.UtcNow,
            FileSizeBytes = audit.FileSizeBytes,
            GenerationTimeMs = audit.GenerationTimeMs,
            ClientIp = audit.ClientIp,
            Success = audit.Success,
            ErrorMessage = audit.ErrorMessage,
            FileName = audit.FileName
        };

        await _reportAuditRepository.AddAsync(entity, CancellationToken.None);

        return entity.Id;
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
