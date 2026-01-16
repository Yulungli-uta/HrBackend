using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using WsUtaSystem.Application.DTOs;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public sealed class HrBalanceRepository : IHrBalanceRepository
    {
        private readonly string _cs;
        private readonly ILogger<HrBalanceRepository> _logger;

        public HrBalanceRepository(IConfiguration cfg, ILogger<HrBalanceRepository> logger)
        {
            _cs = cfg.GetConnectionString("SqlServerConn")
                  ?? throw new InvalidOperationException("Missing connection string: SqlServerConn");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private SqlConnection NewConn() => new SqlConnection(_cs);

        private static object DumpParams(DynamicParameters p)
        {
            // Devuelve algo amigable para log: { Param = valor, ... }
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in p.ParameterNames)
            {
                try { dict[name] = p.Get<object?>(name); }
                catch { dict[name] = "<unreadable>"; }
            }
            return dict;
        }

        private async Task<int> GetTranCountAsync(IDbConnection conn, IDbTransaction? tx)
        {
            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "SELECT @@TRANCOUNT",
                        transaction: tx,
                        commandType: CommandType.Text));
            }
            catch
            {
                return -1; // si no se puede consultar, devuelve -1
            }
        }

        private async Task<SpResult> ExecSpWithStatusAsync(
            IDbConnection conn,
            string spName,
            DynamicParameters p,
            IDbTransaction? tx)
        {
            // Outputs estándar
            p.Add("@StatusCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

            // Log inputs + transacción
            var hasTx = tx is not null;
            var inputsSnapshot = DumpParams(p);

            // Para diagnosticar el error 266: mira el tran count antes y después
            var tranBefore = await GetTranCountAsync(conn, tx);

            _logger.LogInformation(
                "HR SP CALL => {SpName} | HasTx={HasTx} | TranCountBefore={TranBefore} | Inputs={Inputs}",
                spName, hasTx, tranBefore, inputsSnapshot);

            try
            {
                await conn.ExecuteAsync(
                    new CommandDefinition(
                        spName,
                        p,
                        transaction: tx,
                        commandType: CommandType.StoredProcedure));

                var code = p.Get<int>("@StatusCode");
                var msg = p.Get<string>("@Message") ?? "";

                var tranAfter = await GetTranCountAsync(conn, tx);

                _logger.LogInformation(
                    "HR SP RESULT <= {SpName} | StatusCode={StatusCode} | Message={Message} | TranCountAfter={TranAfter}",
                    spName, code, msg, tranAfter);

                return new SpResult(code, msg);
            }
            catch (Exception ex)
            {
                var tranAfter = await GetTranCountAsync(conn, tx);

                _logger.LogError(
                    ex,
                    "HR SP ERROR !! {SpName} | HasTx={HasTx} | TranCountBefore={TranBefore} | TranCountAfter={TranAfter} | Inputs={Inputs}",
                    spName, hasTx, tranBefore, tranAfter, inputsSnapshot);

                throw; // vuelve a lanzar para que lo capture tu middleware
            }
        }

        public async Task<SpResult> AccrueVacationAsync(int employeeId, DateOnly? asOfDate, string mode, int? performedByEmpId, IDbTransaction? tx = null)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeID", employeeId);
            p.Add("@AsOfDate", asOfDate?.ToDateTime(TimeOnly.MinValue));
            p.Add("@Mode", mode);
            p.Add("@PerformedByEmpID", performedByEmpId);

            if (tx is null)
            {
                using var conn = NewConn();
                return await ExecSpWithStatusAsync(conn, "HR.sp_hr_AccrueVacationBalance", p, null);
            }

            return await ExecSpWithStatusAsync(tx.Connection!, "HR.sp_hr_AccrueVacationBalance", p, tx);
        }

        public async Task<SpResult> ReserveVacationAsync(int vacationId, int? performedByEmpId, IDbTransaction? tx = null)
        {
            var p = new DynamicParameters();
            p.Add("@VacationID", vacationId);
            p.Add("@PerformedByEmpID", performedByEmpId);

            if (tx is null)
            {
                using var conn = NewConn();
                return await ExecSpWithStatusAsync(conn, "HR.sp_hr_ReserveVacationBalance", p, null);
            }

            return await ExecSpWithStatusAsync(tx.Connection!, "HR.sp_hr_ReserveVacationBalance", p, tx);
        }

        public async Task<SpResult> ReservePermissionAsync(int permissionId, int? performedByEmpId, IDbTransaction? tx = null)
        {
            var p = new DynamicParameters();
            p.Add("@PermissionID", permissionId);
            p.Add("@PerformedByEmpID", performedByEmpId);

            if (tx is null)
            {
                using var conn = NewConn();
                return await ExecSpWithStatusAsync(conn, "HR.sp_hr_ReservePermissionBalance", p, null);
            }

            return await ExecSpWithStatusAsync(tx.Connection!, "HR.sp_hr_ReservePermissionBalance", p, tx);
        }

        public async Task<SpResult> ConsumeReservationAsync(string reserveSourceId, int? performedByEmpId, IDbTransaction? tx = null)
        {
            var p = new DynamicParameters();
            p.Add("@ReserveSourceID", reserveSourceId);
            p.Add("@PerformedByEmpID", performedByEmpId);

            if (tx is null)
            {
                using var conn = NewConn();
                return await ExecSpWithStatusAsync(conn, "HR.sp_hr_ConsumeReservation", p, null);
            }

            return await ExecSpWithStatusAsync(tx.Connection!, "HR.sp_hr_ConsumeReservation", p, tx);
        }

        public async Task<SpResult> ReleaseReservationAsync(string reserveSourceId, int? performedByEmpId, IDbTransaction? tx = null)
        {
            var p = new DynamicParameters();
            p.Add("@ReserveSourceID", reserveSourceId);
            p.Add("@PerformedByEmpID", performedByEmpId);

            if (tx is null)
            {
                using var conn = NewConn();
                return await ExecSpWithStatusAsync(conn, "HR.sp_hr_ReleaseReservation", p, null);
            }

            return await ExecSpWithStatusAsync(tx.Connection!, "HR.sp_hr_ReleaseReservation", p, tx);
        }

        public async Task<SpResult> ProcessRecoveryAsync(int employeeId, DateOnly startDate, DateOnly endDate, int? performedByEmpId, IDbTransaction? tx = null)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeID", employeeId);
            p.Add("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            p.Add("@EndDate", endDate.ToDateTime(TimeOnly.MinValue));
            p.Add("@PerformedByEmpID", performedByEmpId);

            if (tx is null)
            {
                using var conn = NewConn();
                return await ExecSpWithStatusAsync(conn, "HR.sp_hr_ProcessRecoveryBalance", p, null);
            }

            return await ExecSpWithStatusAsync(tx.Connection!, "HR.sp_hr_ProcessRecoveryBalance", p, tx);
        }

        public async Task<SpResult> DebitRecoveryAsync(int recoveryLogId, int? performedByEmpId, IDbTransaction? tx = null)
        {
            var p = new DynamicParameters();
            p.Add("@RecoveryLogID", recoveryLogId);
            p.Add("@PerformedByEmpID", performedByEmpId);

            if (tx is null)
            {
                using var conn = NewConn();
                return await ExecSpWithStatusAsync(conn, "HR.sp_hr_DebitRecoveryBalance", p, null);
            }

            return await ExecSpWithStatusAsync(tx.Connection!, "HR.sp_hr_DebitRecoveryBalance", p, tx);
        }

        public async Task<(EmployeeBalanceDto balance, IReadOnlyList<MovementDto> movements)> GetBalancesAsync(int employeeId)
        {
            using var conn = NewConn();

            _logger.LogInformation("HR GetBalancesAsync => EmployeeID={EmployeeID}", employeeId);

            using var multi = await conn.QueryMultipleAsync(
                "HR.sp_hr_GetEmployeeBalances",
                new { EmployeeID = employeeId },
                commandType: CommandType.StoredProcedure);

            var balance = await multi.ReadSingleAsync<EmployeeBalanceDto>();
            var movements = (await multi.ReadAsync<MovementDto>()).AsList();

            _logger.LogInformation(
                "HR GetBalancesAsync <= EmployeeID={EmployeeID} | MovementsCount={Count}",
                employeeId, movements.Count);

            return (balance, movements);
        }
    }
}
