using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public class AttendanceCalculationService : IAttendanceCalculationService
{
    private readonly WsUtaSystem.Data.AppDbContext _db;
    private readonly ILogger<AttendanceCalculationService> _logger;

    public AttendanceCalculationService(
        WsUtaSystem.Data.AppDbContext db,
        ILogger<AttendanceCalculationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAttendanceRunRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Processing attendance pipeline from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}",
            fromDate,
            toDate);

        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = "HR.sp_ProcessAttendanceRunRange";
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.CommandTimeout = 3600; // 60 minutos

        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        command.Parameters.Add(new SqlParameter("@Debug", false));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);

        _logger.LogInformation(
            "Finished attendance pipeline from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}",
            fromDate,
            toDate);
    }

    public async Task ProcessAttendanceRunDateAsync(
        DateTime workDate,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Processing attendance pipeline for date {WorkDate:yyyy-MM-dd}",
            workDate);

        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = "HR.sp_ProcessAttendanceRunDate";
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.CommandTimeout = 3600; // 60 minutos

        command.Parameters.Add(new SqlParameter("@WorkDate", workDate.Date));
        command.Parameters.Add(new SqlParameter("@Debug", false));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);

        _logger.LogInformation(
            "Finished attendance pipeline for date {WorkDate:yyyy-MM-dd}",
            workDate);
    }

    [Obsolete("Use ProcessAttendanceRunRangeAsync instead.")]
    public Task ProcessAttendanceRange(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default)
    {
        return ProcessAttendanceRunRangeAsync(fromDate, toDate, ct);
    }

    [Obsolete("Legacy method. Use the attendance pipeline instead.")]
    public async Task CalculateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Legacy attendance calculate range from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}",
            fromDate,
            toDate,
            employeeId?.ToString() ?? "All");

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
    }

    [Obsolete("Legacy method. Night minutes are now processed inside the attendance pipeline.")]
    public async Task CalculateNightMinutesAsync(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Legacy night minutes calculation from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}",
            fromDate,
            toDate,
            employeeId?.ToString() ?? "All");

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
    }

    [Obsolete("Legacy method. Justifications are now processed inside the attendance pipeline.")]
    public async Task ProcessApplyJustification(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Legacy justifications application from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}",
            fromDate,
            toDate,
            employeeId?.ToString() ?? "All");

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

    [Obsolete("Legacy method. Overtime and recovery are now processed inside the attendance pipeline.")]
    public async Task ProcessApplyOvertimeRecovery(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Legacy overtime/recovery application from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd} for EmployeeID={EmployeeID}",
            fromDate,
            toDate,
            employeeId?.ToString() ?? "All");

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