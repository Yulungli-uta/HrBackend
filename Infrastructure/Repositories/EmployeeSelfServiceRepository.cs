using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Application.DTOs.EmployeeSelfService;
using WsUtaSystem.Application.Interfaces.Repositories;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <inheritdoc cref="IEmployeeSelfServiceRepository"/>
public sealed class EmployeeSelfServiceRepository : IEmployeeSelfServiceRepository
{
    private readonly string _cs;
    private readonly ILogger<EmployeeSelfServiceRepository> _logger;

    public EmployeeSelfServiceRepository(IConfiguration cfg, ILogger<EmployeeSelfServiceRepository> logger)
    {
        _cs = cfg.GetConnectionString("SqlServerConn")
              ?? throw new InvalidOperationException("Missing connection string: SqlServerConn");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EmployeeSelfServiceSummaryRawData> GetSummaryDataAsync(int employeeId, CancellationToken ct = default)
    {
        using var conn = new SqlConnection(_cs);

        _logger.LogInformation("HR GetEmployeeSelfServiceSummary => EmployeeID={EmployeeID}", employeeId);

        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(
                "HR.sp_GetEmployeeSelfServiceSummary",
                new { EmployeeID = employeeId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        // El orden de lectura DEBE coincidir exactamente con el orden de los SELECT
        // dentro del stored procedure (Database/hr/11_employee_self_service.sql).
        var vacationAvailableMin = await multi.ReadFirstOrDefaultAsync<int?>();
        var recentPermissions = (await multi.ReadAsync<EmployeeSelfServicePermissionRow>()).AsList();
        var pendingPermissionsCount = await multi.ReadSingleAsync<int>();
        var recentVacations = (await multi.ReadAsync<EmployeeSelfServiceVacationRow>()).AsList();
        var recentCertificates = (await multi.ReadAsync<EmployeeCertificateSummaryDto>()).AsList();
        var recentInternalRequests = (await multi.ReadAsync<EmployeeSelfServiceInternalRequestRow>()).AsList();
        var lastPunch = await multi.ReadFirstOrDefaultAsync<EmployeeSelfServiceLastPunchRow>();
        var pendingJustificationsCount = await multi.ReadSingleAsync<int>();

        _logger.LogInformation(
            "HR GetEmployeeSelfServiceSummary <= EmployeeID={EmployeeID} | Permissions={PermCount} | Vacations={VacCount} | Certificates={CertCount} | InternalRequests={ReqCount}",
            employeeId, recentPermissions.Count, recentVacations.Count, recentCertificates.Count, recentInternalRequests.Count);

        return new EmployeeSelfServiceSummaryRawData(
            vacationAvailableMin,
            pendingPermissionsCount,
            recentPermissions,
            recentVacations,
            recentCertificates,
            recentInternalRequests,
            lastPunch,
            pendingJustificationsCount);
    }
}
