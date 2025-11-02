using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace WsUtaSystem.Application.Services;

public class OvertimePriceService : IOvertimePriceService
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public OvertimePriceService(WsUtaSystem.Data.AppDbContext db)
    {
        _db = db;
    }

    public async Task CalculateOvertimePriceAsync(string period, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Overtime_Price";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Period", period));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }
}

