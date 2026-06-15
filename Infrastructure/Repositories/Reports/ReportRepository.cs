using Dapper;
using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;

namespace WsUtaSystem.Infrastructure.Repositories.Reports;

/// <summary>
/// Repositorio para obtener datos de reportes legacy (v1) usando stored procedures.
/// Los reportes v2 usan IReportSource con servicios de aplicación (EF Core).
/// </summary>
public class ReportRepository : IReportRepository
{
    private readonly string _connectionString;

    public ReportRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConn")
            ?? throw new InvalidOperationException("Connection string 'SqlServerConn' not found.");
    }

    /// <summary>Obtiene datos del reporte de empleados para generadores v1.</summary>
    public async Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new
        {
            StartDate    = filter.StartDate,
            EndDate      = filter.EndDate,
            DepartmentId = filter.DepartmentId,
            EmployeeType = filter.EmployeeType,
            IsActive     = filter.IsActive
        };
        return await connection.QueryAsync<EmployeeReportDto>(
            "[HR].[sp_GetEmployeesReport]", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <summary>Obtiene datos del reporte de asistencia para generadores v1.</summary>
    public async Task<IEnumerable<AttendanceReportDto>> GetAttendanceReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new
        {
            StartDate    = filter.StartDate ?? DateTime.Now.AddMonths(-1),
            EndDate      = filter.EndDate   ?? DateTime.Now,
            EmployeeId   = filter.EmployeeId,
            DepartmentId = filter.DepartmentId,
        };
        return await connection.QueryAsync<AttendanceReportDto>(
            "[HR].[sp_GetAttendanceReport]", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <summary>Obtiene datos del reporte de dependencias para generadores v1.</summary>
    public async Task<IEnumerable<DepartmentReportDto>> GetDepartmentsReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new { IncludeInactive = filter.IncludeInactive ?? false };
        return await connection.QueryAsync<DepartmentReportDto>(
            "[HR].[sp_GetDepartmentsReport]", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <summary>Obtiene resumen de asistencia para AttendanceSummaryReportSource.</summary>
    public async Task<IEnumerable<AttendanceSumaryDto>> GetAttendanceSumaryReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new
        {
            StartDate    = filter.StartDate ?? DateTime.Now.AddMonths(-1),
            EndDate      = filter.EndDate   ?? DateTime.Now,
            EmployeeId   = filter.EmployeeId,
            EmployeeType = filter.EmployeeType
        };
        return await connection.QueryAsync<AttendanceSumaryDto>(
            "[HR].[sp_GetReportAttendanceSumary]", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }
}
