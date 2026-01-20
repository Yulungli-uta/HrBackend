using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;              


namespace WsUtaSystem.Application.Services;

public class AttendanceCalculationService : IAttendanceCalculationService
{
    private readonly WsUtaSystem.Data.AppDbContext _db;
    private readonly ILogger<AttendanceCalculationService> _logger;

    public AttendanceCalculationService(WsUtaSystem.Data.AppDbContext db, ILogger<AttendanceCalculationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CalculateRangeAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("*******************JobExecucion - Calculating attendance from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}", fromDate, toDate, employeeId?.ToString() ?? "All");
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Attendance_CalculateRange";
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        command.Parameters.Add(new SqlParameter("@EmployeeID", (object?)employeeId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
        _logger.LogInformation("*******************JobExecucion - Finished calculating attendance from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}", fromDate, toDate, employeeId?.ToString() ?? "All");
    }

    public async Task CalculateNightMinutesAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("*******************JobExecucion - Calculating night minutes from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}", fromDate, toDate, employeeId?.ToString() ?? "All");
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Attendance_CalcNightMinutes";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        command.Parameters.Add(new SqlParameter("@EmployeeID", (object?)employeeId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
        _logger.LogInformation("*******************JobExecucion - Finished calculating night minutes from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}", fromDate, toDate, employeeId?.ToString() ?? "All");
    }

    public async Task ProcessAttendanceRange(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        _logger.LogInformation("*******************JobExecucion - Processing attendance from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}", fromDate, toDate);
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_ProcessAttendanceRange";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        //command.Parameters.Add(new SqlParameter("@EmployeeID", (object?)employeeId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
        _logger.LogInformation("*******************JobExecucion - Finished processing attendance from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}", fromDate, toDate);
    }

    public async Task ProcessApplyJustification(DateTime fromDate, DateTime toDate, int? employeeId = null,  CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Justifications_Apply";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        command.Parameters.Add(new SqlParameter("@EmployeeID", (object?)employeeId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task ProcessApplyOvertimeRecovery(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Overtime_Calculate";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        command.Parameters.Add(new SqlParameter("@EmployeeID", (object?)employeeId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }
}

