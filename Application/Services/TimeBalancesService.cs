using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class TimeBalancesService : Service<TimeBalances, int>, ITimeBalancesService
{

    private readonly WsUtaSystem.Data.AppDbContext _db;
    private readonly ILogger<TimeBalancesService> _logger;
    private readonly IEmployeesService _employees;
    public TimeBalancesService(
        ITimeBalancesRepository repo,
        AppDbContext db,
        ILogger<TimeBalancesService> logger,
        IEmployeesService employees
        ) : base(repo) 
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _employees = employees ?? throw new ArgumentNullException(nameof(employees));
    }

    public async Task CalculateAccrueVacationBalance(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Calculating accrue vacation balance from {FromDate} to {ToDate} for EmployeeID: {EmployeeID}",
            fromDate.ToString("yyyy-MM-dd"),
            toDate.ToString("yyyy-MM-dd"),
            employeeId?.ToString() ?? "All Employees");

        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_hr_AccrueVacationBalance";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        command.Parameters.Add(new SqlParameter("@EmployeeID", (object?)employeeId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task CalculateAccrueVacationBalanceAllEmployees(
        DateTime asOfDate,
        string mode,
        int? performedByEmpId,
        CancellationToken ct = default)
    {
        // Traer empleados
        var employees = await _employees.GetAllAsync(ct);

        // Ajusta nombres de propiedades según tu modelo real
        var activeEmployeeIds = employees
            .Where(e => e.IsActive)        // <-- si tu propiedad es distinta, cámbiala
            .Select(e => e.EmployeeId)     // <-- si tu ID se llama distinto, cámbialo
            .ToList();

        // PUNTO 3.4: además de los activos, incluir empleados inactivos cuyo
        // último contrato (sin addendum que lo extienda) haya terminado DENTRO
        // del mes que se está acreditando, para que reciban su liquidación
        // proporcional final (HR.sp_hr_AccrueVacationBalance modo MONTHLY ya
        // prorratea contra ese EndDate). No se incluyen inactivos de meses
        // anteriores — esos ya no deben seguir acreditando. Reutiliza la misma
        // verificación de "contrato realmente terminado" que
        // ContractExpirationService.ProcessExpiredContractsAsync (sin addendum
        // vigente con EndDate posterior). Solo aplica en modo MONTHLY, que es
        // el único que hoy tiene la lógica de prorrateo por EndDate en el SP.
        var employeeIdsToProcess = activeEmployeeIds;

        if (string.Equals(mode, "MONTHLY", StringComparison.OrdinalIgnoreCase))
        {
            var periodStart = new DateTime(asOfDate.Year, asOfDate.Month, 1);
            var periodEndExclusive = periodStart.AddMonths(1);

            var terminatedPersonIds = await _db.Set<Contracts>()
                .AsNoTracking()
                .Where(c => c.EndDate >= periodStart && c.EndDate < periodEndExclusive
                         && !_db.Set<Contracts>().Any(a => a.ParentID == c.ContractID && a.EndDate >= c.EndDate))
                .Select(c => c.PersonID)
                .Distinct()
                .ToListAsync(ct);

            var terminatedThisMonthIds = employees
                .Where(e => !e.IsActive && terminatedPersonIds.Contains(e.PersonID))
                .Select(e => e.EmployeeId)
                .ToList();

            if (terminatedThisMonthIds.Count > 0)
            {
                _logger.LogInformation(
                    "AccrueVacationBalance incluyendo {Count} empleado(s) inactivo(s) con baja dentro del período {PeriodStart:yyyy-MM}",
                    terminatedThisMonthIds.Count, periodStart);
            }

            employeeIdsToProcess = activeEmployeeIds.Concat(terminatedThisMonthIds).Distinct().ToList();
        }

        _logger.LogInformation(
            "AccrueVacationBalance ALL employees count={Count} asOfDate={AsOfDate:yyyy-MM-dd} mode={Mode}",
            employeeIdsToProcess.Count, asOfDate, mode);

        foreach (var empId in employeeIdsToProcess)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var (status, message) = await CalculateAccrueVacationBalanceFinal(
                    employeeId: empId,
                    asOfDate: asOfDate,
                    mode: mode,
                    performedByEmpId: performedByEmpId,
                    ct: ct);

                // Opcional: log por empleado
                _logger.LogDebug(
                    "AccrueVacationBalance employeeId={EmployeeId} status={Status} message={Message}",
                    empId, status, message);
            }
            catch (Exception ex)
            {
                // Decide si quieres continuar con los demás o detener todo
                _logger.LogError(ex, "AccrueVacationBalance failed for employeeId={EmployeeId}", empId);
                // Si quieres que falle todo el job, descomenta:
                // throw;
            }
        }
    }

    //public async Task<(int StatusCode, string Message)> CalculateAccrueVacationBalanceFinal(
    //     int employeeId,
    //     DateTime asOfDate,
    //     string mode,
    //     int? performedByEmpId,
    //     CancellationToken ct = default)
    //{
       
    //}

    public async Task<(int StatusCode, string Message)> CalculateAccrueVacationBalanceFinal(int? employeeId, DateTime asOfDate, string mode, int? performedByEmpId, CancellationToken ct = default)
    {
        _logger.LogInformation(
           "AccrueVacationBalance employeeId={EmployeeId} asOfDate={AsOfDate:yyyy-MM-dd} mode={Mode} performedBy={PerformedBy}",
           employeeId, asOfDate, mode, performedByEmpId);

        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_hr_AccrueVacationBalance";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        // INPUTS (nombres EXACTOS como en el SP)
        command.Parameters.Add(new SqlParameter("@EmployeeID", System.Data.SqlDbType.Int) { Value = employeeId });
        command.Parameters.Add(new SqlParameter("@AsOfDate", System.Data.SqlDbType.Date) { Value = asOfDate.Date });
        command.Parameters.Add(new SqlParameter("@Mode", System.Data.SqlDbType.VarChar, 10) { Value = mode ?? "TOTAL" });

        var pPerformed = new SqlParameter("@PerformedByEmpID", System.Data.SqlDbType.Int);
        pPerformed.Value = performedByEmpId.HasValue ? performedByEmpId.Value : DBNull.Value;
        command.Parameters.Add(pPerformed);

        // OUTPUTS
        var pStatus = new SqlParameter("@StatusCode", System.Data.SqlDbType.Int)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(pStatus);

        var pMessage = new SqlParameter("@Message", System.Data.SqlDbType.NVarChar, 500)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        command.Parameters.Add(pMessage);

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);

        var statusCode = (pStatus.Value == DBNull.Value) ? 0 : (int)pStatus.Value;
        var message = (pMessage.Value == DBNull.Value) ? string.Empty : (string)pMessage.Value;

        _logger.LogInformation("AccrueVacationBalance result statusCode={StatusCode} message={Message}", statusCode, message);

        return (statusCode, message);
    }
}

