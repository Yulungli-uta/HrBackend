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
    private const string ContractTypeCategory = "CONTRACT_TYPE";
    private const string CodigoTrabajoName = "Código Trabajo";
    private const string LoesName = "LOES";

    private readonly WsUtaSystem.Data.AppDbContext _db;
    private readonly ILogger<TimeBalancesService> _logger;
    private readonly IEmployeesService _employees;
    private readonly IHrBalanceService _hrBalanceService;
    public TimeBalancesService(
        ITimeBalancesRepository repo,
        AppDbContext db,
        ILogger<TimeBalancesService> logger,
        IEmployeesService employees,
        IHrBalanceService hrBalanceService
        ) : base(repo)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _employees = employees ?? throw new ArgumentNullException(nameof(employees));
        _hrBalanceService = hrBalanceService ?? throw new ArgumentNullException(nameof(hrBalanceService));
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

        // Régimen Código de Trabajo: rama aparte, no toca la de LOSEP de arriba.
        // sp_hr_AccrueVacationBalance_CT solo soporta MONTHLY (ver comentario del SP).
        if (string.Equals(mode, "MONTHLY", StringComparison.OrdinalIgnoreCase))
        {
            await AccrueCTBranchAsync(employeeIdsToProcess, asOfDate, performedByEmpId, ct);
            await AccrueLOESBranchAsync(employeeIdsToProcess, asOfDate, performedByEmpId, ct);
        }
    }

    /// <summary>
    /// Acredita el proporcional mensual a los empleados con régimen "LOES" activo.
    /// sp_hr_AccrueVacationBalance_LOES ya existía pero ningún job lo llamaba nunca —
    /// hallazgo de esta sesión, ver Database/MULTI_REGIME_EMPLOYEES.md. Rama aparte, no
    /// toca la de LOSEP de arriba.
    /// </summary>
    private async Task AccrueLOESBranchAsync(List<int> employeeIds, DateTime asOfDate, int? performedByEmpId, CancellationToken ct)
    {
        var loesRegimeId = await _db.RefTypes.AsNoTracking()
            .Where(r => r.Category == ContractTypeCategory && r.Name == LoesName && r.IsActive)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (loesRegimeId is null or 0)
        {
            _logger.LogWarning("AccrueVacationBalance LOES: no existe régimen activo '{Name}' en ref_Types, se omite la rama.", LoesName);
            return;
        }

        var loesEmployeeIds = await _db.Set<EmployeeLaborRegime>().AsNoTracking()
            .Where(r => r.LaborRegimeId == loesRegimeId && r.IsActive && employeeIds.Contains(r.EmployeeId))
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("AccrueVacationBalance LOES: {Count} empleado(s) con régimen activo.", loesEmployeeIds.Count);

        foreach (var empId in loesEmployeeIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var result = await _hrBalanceService.RunMonthlyAccrualLOESAsync(empId, DateOnly.FromDateTime(asOfDate), performedByEmpId);

                _logger.LogDebug(
                    "AccrueVacationBalance LOES employeeId={EmployeeId} statusCode={StatusCode} message={Message}",
                    empId, result.StatusCode, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccrueVacationBalance LOES failed for employeeId={EmployeeId}", empId);
            }
        }
    }

    /// <summary>
    /// Acredita el proporcional mensual a los empleados de employeeIds que tengan
    /// régimen "Código Trabajo" activo (resuelto por Name, no por Id). Una falla en
    /// un empleado no detiene a los demás — mismo criterio que la rama LOSEP.
    /// </summary>
    private async Task AccrueCTBranchAsync(List<int> employeeIds, DateTime asOfDate, int? performedByEmpId, CancellationToken ct)
    {
        var ctRegimeId = await _db.RefTypes.AsNoTracking()
            .Where(r => r.Category == ContractTypeCategory && r.Name == CodigoTrabajoName && r.IsActive)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (ctRegimeId is null or 0)
        {
            _logger.LogWarning("AccrueVacationBalance CT: no existe régimen activo '{Name}' en ref_Types, se omite la rama.", CodigoTrabajoName);
            return;
        }

        var ctEmployeeIds = await _db.Set<EmployeeLaborRegime>().AsNoTracking()
            .Where(r => r.LaborRegimeId == ctRegimeId && r.IsActive && employeeIds.Contains(r.EmployeeId))
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("AccrueVacationBalance CT: {Count} empleado(s) con régimen activo.", ctEmployeeIds.Count);

        foreach (var empId in ctEmployeeIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var result = await _hrBalanceService.RunMonthlyAccrualCTAsync(empId, DateOnly.FromDateTime(asOfDate), performedByEmpId);

                _logger.LogDebug(
                    "AccrueVacationBalance CT employeeId={EmployeeId} statusCode={StatusCode} message={Message}",
                    empId, result.StatusCode, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccrueVacationBalance CT failed for employeeId={EmployeeId}", empId);
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

