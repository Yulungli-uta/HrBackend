using Dapper;
using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;

namespace WsUtaSystem.Infrastructure.Repositories.Reports;

/// <summary>
/// Repositorio para obtener datos de reportes usando stored procedures
/// </summary>
public class ReportRepository : IReportRepository
{
    private readonly string _connectionString;

    public ReportRepository(IConfiguration configuration)
    {
        // Para debug - ver todas las connection strings
        var allConnectionStrings = configuration.GetSection("ConnectionStrings").GetChildren();
        foreach (var conn in allConnectionStrings)
        {
            Console.WriteLine($"Connection String Key: {conn.Key}, Value: {conn.Value}");
        }

        _connectionString = configuration.GetConnectionString("SqlServerConn")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings' not found.");
    }

    public async Task<IEnumerable<EmployeeReportDto>> GetEmployeesReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var parameters = new
        {
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            DepartmentId = filter.DepartmentId,
            //FacultyId = filter.FacultyId,
            EmployeeType = filter.EmployeeType,
            IsActive = filter.IsActive
        };

        return await connection.QueryAsync<EmployeeReportDto>(
            "[HR].[sp_GetEmployeesReport]",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<AttendanceReportDto>> GetAttendanceReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var parameters = new
        {
            StartDate = filter.StartDate ?? DateTime.Now.AddMonths(-1),
            EndDate = filter.EndDate ?? DateTime.Now,
            EmployeeId = filter.EmployeeId,
            DepartmentId = filter.DepartmentId,
            //FacultyId = filter.FacultyId
        };

        return await connection.QueryAsync<AttendanceReportDto>(
            "[HR].[sp_GetAttendanceReport]",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<DepartmentReportDto>> GetDepartmentsReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);
        
        var parameters = new
        {
           //FacultyId = filter.FacultyId,
            IncludeInactive = filter.IncludeInactive ?? false
        };

        return await connection.QueryAsync<DepartmentReportDto>(
            "[HR].[sp_GetDepartmentsReport]",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<AttendanceSumaryDto>> GetAttendanceSumaryReportDataAsync(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new
        {
            StartDate = filter.StartDate ?? DateTime.Now.AddMonths(-1),
            EndDate = filter.EndDate ?? DateTime.Now,
            EmployeeId = filter.EmployeeId,
            EmployeeType = filter.EmployeeType
        };

        return await connection.QueryAsync<AttendanceSumaryDto>(
            "[HR].[sp_GetReportAttendanceSumary]",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<AttendanceReportDto>> GetReportAttendanceSumary(ReportFilterDto filter)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new
        {
            StartDate = filter.StartDate ?? DateTime.Now.AddMonths(-1),
            EndDate = filter.EndDate ?? DateTime.Now,
            EmployeeId = filter.EmployeeId,
            EmployeeType = filter.EmployeeType            
        };

        return await connection.QueryAsync<AttendanceReportDto>(
            "[HR].[sp_GetAttendanceReport]",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure
        );
    }
}
