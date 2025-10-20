using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public class PayrollSubsidiesService : IPayrollSubsidiesService
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    public PayrollSubsidiesService(WsUtaSystem.Data.AppDbContext db)
    {
        _db = db;
    }

    public async Task CalculateSubsidiesAsync(string period, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Payroll_Subsidies";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Period", period));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }
}

