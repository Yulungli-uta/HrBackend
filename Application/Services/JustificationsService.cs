using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class JustificationsService : Service<PunchJustifications, int>, IJustificationsService
{
    private readonly WsUtaSystem.Data.AppDbContext _db;

    private readonly IPunchJustificationsRepository _repository;
    public JustificationsService(IPunchJustificationsRepository repo) : base(repo)
    {
        _repository = repo;
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

    public async Task<IEnumerable<PunchJustifications>> GetByBossEmployeeId(int BossEmployeeId, CancellationToken ct)
    {
        return await _repository.GetByBossEmployeeId(BossEmployeeId, ct);
    }

    public async Task<IEnumerable<PunchJustifications>> GetByEmployeeId(int EmployeeId, CancellationToken ct)
    {
        return await _repository.GetByEmployeeId(EmployeeId, ct);
    }
}

