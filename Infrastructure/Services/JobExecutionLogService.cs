using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IJobExecutionLogService"/> vía Dapper directo contra
/// HR.sp_JobExecutionLog_Start / HR.sp_JobExecutionLog_Finish. Es infraestructura técnica
/// de logging, no dominio CRUD, por eso no pasa por EF Core.
/// </summary>
public class JobExecutionLogService : IJobExecutionLogService
{
    private readonly string _connectionString;

    public JobExecutionLogService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConn")
            ?? throw new InvalidOperationException("Connection string 'SqlServerConn' not found.");
    }

    /// <inheritdoc/>
    public async Task<long> StartAsync(string jobName, string source, CancellationToken ct = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@JobName", jobName);
        parameters.Add("@Source", source);
        parameters.Add("@LogID", dbType: DbType.Int64, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "HR.sp_JobExecutionLog_Start",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct);

        await connection.ExecuteAsync(command);

        return parameters.Get<long>("@LogID");
    }

    /// <inheritdoc/>
    public async Task FinishAsync(long logId, string status, string? errorMessage = null, CancellationToken ct = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var command = new CommandDefinition(
            "HR.sp_JobExecutionLog_Finish",
            new
            {
                LogID = logId,
                Status = status,
                ErrorMessage = errorMessage
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct);

        await connection.ExecuteAsync(command);
    }
}
