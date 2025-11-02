using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace WsUtaSystem.Application.Services;

public class JustificationsService : IJustificationsService
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public JustificationsService(WsUtaSystem.Data.AppDbContext db)
    {
        _db = db;
    }

    public async Task ApplyJustificationsAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default)
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
}

