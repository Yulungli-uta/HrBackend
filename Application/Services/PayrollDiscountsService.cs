using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace WsUtaSystem.Application.Services;

public class PayrollDiscountsService : IPayrollDiscountsService
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public PayrollDiscountsService(WsUtaSystem.Data.AppDbContext db)
    {
        _db = db;
    }

    public async Task CalculateDiscountsAsync(string period, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Payroll_Discounts";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Period", period));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }
}

