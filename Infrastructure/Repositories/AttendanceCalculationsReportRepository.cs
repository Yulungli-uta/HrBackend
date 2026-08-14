using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IAttendanceCalculationsReportRepository"/> usando
/// EF Core + LINQ con proyección directa a DTOs.
/// </summary>
/// <remarks>
/// <para>
/// Principio DIP: depende de la abstracción <see cref="AppDbContext"/> inyectada,
/// no de una conexión SQL directa.
/// </para>
/// <para>
/// Todas las consultas usan <c>AsNoTracking()</c> para maximizar el rendimiento
/// en operaciones de solo lectura (reportes).
/// </para>
/// <para>
/// El join a <c>Employees</c> y <c>People</c> permite obtener el nombre completo
/// y la cédula del empleado sin necesidad de consultas adicionales.
/// El join a <c>Departments</c> permite filtrar por departamento cuando se especifica.
/// </para>
/// </remarks>
public sealed class AttendanceCalculationsReportRepository : IAttendanceCalculationsReportRepository
{
    private readonly AppDbContext _db;

    public AttendanceCalculationsReportRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LatenessReportDto>> GetLatenessDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var startDate = filter.StartDate.HasValue
            ? DateOnly.FromDateTime(filter.StartDate.Value)
            : DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

        var endDate = filter.EndDate.HasValue
            ? DateOnly.FromDateTime(filter.EndDate.Value)
            : DateOnly.FromDateTime(DateTime.Today);

        var query = from calc in _db.AttendanceCalculations.AsNoTracking()
                    join emp in _db.Employees.AsNoTracking()
                        on calc.EmployeeId equals emp.EmployeeId
                    join person in _db.People.AsNoTracking()
                        on emp.PersonID equals person.PersonId
                    where calc.WorkDate >= startDate
                       && calc.WorkDate <= endDate
                       && (calc.MinutesLate > 0 || calc.TardinessMin > 0)
                    select new { calc, emp, person };

        // Filtro opcional por empleado
        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        // Filtro opcional por departamento
        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.emp.DepartmentId == filter.DepartmentId.Value);

        var result = await query
            .OrderBy(x => x.person.LastName)
            .ThenBy(x => x.person.FirstName)
            .ThenBy(x => x.calc.WorkDate)
            .Select(x => new LatenessReportDto
            {
                EmployeeId            = x.calc.EmployeeId,
                IdCard                = x.person.IdCard,
                FullName              = x.person.LastName + " " + x.person.FirstName,
                WorkDate              = x.calc.WorkDate,
                MinutesLate           = x.calc.MinutesLate,
                TardinessMin          = x.calc.TardinessMin,
                ScheduledEntryTime    = x.calc.ScheduledEntryTime,
                FirstPunchIn          = x.calc.FirstPunchIn,
                EarlyLeaveMinutes     = x.calc.EarlyLeaveMinutes,
                Status                = x.calc.Status,
                HasJustification      = x.calc.HasJustification,
                JustificationMinutes  = x.calc.JustificationMinutes,
                CalculationVersion    = x.calc.CalculationVersion
            })
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OvertimeReportDto>> GetOvertimeDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var startDate = filter.StartDate.HasValue
            ? DateOnly.FromDateTime(filter.StartDate.Value)
            : DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

        var endDate = filter.EndDate.HasValue
            ? DateOnly.FromDateTime(filter.EndDate.Value)
            : DateOnly.FromDateTime(DateTime.Today);

        var query = from calc in _db.AttendanceCalculations.AsNoTracking()
                    join emp in _db.Employees.AsNoTracking()
                        on calc.EmployeeId equals emp.EmployeeId
                    join person in _db.People.AsNoTracking()
                        on emp.PersonID equals person.PersonId
                    where calc.WorkDate >= startDate
                       && calc.WorkDate <= endDate
                       && (calc.OvertimeMinutes > 0
                           || calc.NightMinutes > 0
                           || calc.HolidayMinutes > 0
                           || calc.OffScheduleMin > 0)
                    select new { calc, emp, person };

        // Filtro opcional por empleado
        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        // Filtro opcional por departamento
        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.emp.DepartmentId == filter.DepartmentId.Value);

        var result = await query
            .OrderBy(x => x.person.LastName)
            .ThenBy(x => x.person.FirstName)
            .ThenBy(x => x.calc.WorkDate)
            .Select(x => new OvertimeReportDto
            {
                EmployeeId          = x.calc.EmployeeId,
                IdCard              = x.person.IdCard,
                FullName            = x.person.LastName + " " + x.person.FirstName,
                WorkDate            = x.calc.WorkDate,
                TotalWorkedMinutes  = x.calc.TotalWorkedMinutes,
                OvertimeMinutes     = x.calc.OvertimeMinutes,
                NightMinutes        = x.calc.NightMinutes,
                HolidayMinutes      = x.calc.HolidayMinutes,
                OffScheduleMin      = x.calc.OffScheduleMin,
                RegularMinutes      = x.calc.RegularMinutes,
                ScheduledMinutes    = x.calc.ScheduledMinutes,
                ScheduledEntryTime  = x.calc.ScheduledEntryTime,
                ScheduledExitTime   = x.calc.ScheduledExitTime,
                FirstPunchIn        = x.calc.FirstPunchIn,
                LastPunchOut        = x.calc.LastPunchOut,
                Status              = x.calc.Status,
                CalculationVersion  = x.calc.CalculationVersion
            })
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AttendanceCrossReportDto>> GetAttendanceCrossDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var startDate = filter.StartDate.HasValue
            ? DateOnly.FromDateTime(filter.StartDate.Value)
            : DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

        var endDate = filter.EndDate.HasValue
            ? DateOnly.FromDateTime(filter.EndDate.Value)
            : DateOnly.FromDateTime(DateTime.Today);

        var query = from calc in _db.AttendanceCalculations.AsNoTracking()
                    join emp in _db.Employees.AsNoTracking()
                        on calc.EmployeeId equals emp.EmployeeId
                    join person in _db.People.AsNoTracking()
                        on emp.PersonID equals person.PersonId
                    where calc.WorkDate >= startDate
                       && calc.WorkDate <= endDate
                    select new { calc, emp, person };

        // Filtro opcional por empleado
        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        // Filtro opcional por departamento
        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.emp.DepartmentId == filter.DepartmentId.Value);

        var result = await query
            .OrderBy(x => x.person.LastName)
            .ThenBy(x => x.person.FirstName)
            .ThenBy(x => x.calc.WorkDate)
            .Select(x => new AttendanceCrossReportDto
            {
                EmployeeId              = x.calc.EmployeeId,
                IdCard                  = x.person.IdCard,
                FullName                = x.person.LastName + " " + x.person.FirstName,
                WorkDate                = x.calc.WorkDate,
                TotalWorkedMinutes      = x.calc.TotalWorkedMinutes,
                RegularMinutes          = x.calc.RegularMinutes,
                OvertimeMinutes         = x.calc.OvertimeMinutes,
                ScheduledMinutes        = x.calc.ScheduledMinutes,
                AbsentMinutes           = x.calc.AbsentMinutes,
                PermissionMinutes       = x.calc.PermissionMinutes,
                HasPermission           = x.calc.HasPermission,
                VacationMinutes         = x.calc.VacationMinutes,
                HasVacation             = x.calc.HasVacation,
                JustificationMinutes    = x.calc.JustificationMinutes,
                HasJustification        = x.calc.HasJustification,
                MedicalLeaveMinutes     = x.calc.MedicalLeaveMinutes,
                HasMedicalLeave         = x.calc.HasMedicalLeave,
                PaidLeaveMinutes        = x.calc.PaidLeaveMinutes,
                UnpaidLeaveMinutes      = x.calc.UnpaidLeaveMinutes,
                VacationDeductedMinutes = x.calc.VacationDeductedMinutes,
                RecoveredMinutes        = x.calc.RecoveredMinutes,
                TardinessMin            = x.calc.TardinessMin,
                EarlyLeaveMinutes       = x.calc.EarlyLeaveMinutes,
                FoodSubsidy             = x.calc.FoodSubsidy,
                Status                  = x.calc.Status,
                HasManualAdjustment     = x.calc.HasManualAdjustment,
                CalculationVersion      = x.calc.CalculationVersion
            })
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AttendanceReportDto>> GetAttendanceDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var startDate = filter.StartDate.HasValue
            ? DateOnly.FromDateTime(filter.StartDate.Value)
            : DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

        var endDate = filter.EndDate.HasValue
            ? DateOnly.FromDateTime(filter.EndDate.Value)
            : DateOnly.FromDateTime(DateTime.Today);

        var query = from calc in _db.AttendanceCalculations.AsNoTracking()
                    join emp in _db.Employees.AsNoTracking()
                        on calc.EmployeeId equals emp.EmployeeId
                    join person in _db.People.AsNoTracking()
                        on emp.PersonID equals person.PersonId
                    join dept in _db.Departments.AsNoTracking()
                        on emp.DepartmentId equals dept.DepartmentId into deptGroup
                    from dept in deptGroup.DefaultIfEmpty()
                    where calc.WorkDate >= startDate && calc.WorkDate <= endDate
                    select new { calc, emp, person, dept };

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.emp.DepartmentId == filter.DepartmentId.Value);

        var result = await query
            .OrderBy(x => x.person.LastName)
            .ThenBy(x => x.person.FirstName)
            .ThenBy(x => x.calc.WorkDate)
            .Select(x => new AttendanceReportDto
            {
                AttendanceDate       = x.calc.WorkDate.ToDateTime(TimeOnly.MinValue),
                EmployeeId           = x.calc.EmployeeId,
                EmployeeName         = x.person.LastName + " " + x.person.FirstName,
                IdentificationNumber = x.person.IdCard,
                DepartmentName       = x.dept != null ? x.dept.Name : string.Empty,
                CheckIn              = x.calc.FirstPunchIn,
                CheckOut             = x.calc.LastPunchOut,
                HoursWorked          = x.calc.TotalWorkedMinutes / 60m,
                Status               = x.calc.TardinessMin > 0 ? "Tardanza"
                                        : x.calc.FirstPunchIn == null ? "Sin Entrada"
                                        : x.calc.LastPunchOut == null ? "Sin Salida"
                                        : "Normal",
            })
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FoodSubsidySummaryReportDto>> GetFoodSubsidySummaryDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var startDate = filter.StartDate.HasValue
            ? DateOnly.FromDateTime(filter.StartDate.Value)
            : DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

        var endDate = filter.EndDate.HasValue
            ? DateOnly.FromDateTime(filter.EndDate.Value)
            : DateOnly.FromDateTime(DateTime.Today);

        var query = from calc in _db.AttendanceCalculations.AsNoTracking()
                    join ved in _db.vwEmployeeDetails.AsNoTracking()
                        on calc.EmployeeId equals ved.EmployeeID
                    where calc.WorkDate >= startDate && calc.WorkDate <= endDate
                    select new { calc, ved };

        // Filtro opcional por empleado
        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        // Filtro opcional por dependencia
        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.ved.DepartmentID == filter.DepartmentId.Value);

        // Filtro opcional por cédula exacta
        if (!string.IsNullOrWhiteSpace(filter.Identification))
            query = query.Where(x => x.ved.IDCard == filter.Identification);

        // Filtro opcional por régimen laboral. No se aplica por defecto: el flag
        // FoodSubsidy ya solo se activa para Código de Trabajo, así que el resto de
        // empleados queda excluido naturalmente (0 días) sin necesidad de forzar este filtro.
        // Mismo criterio que VwEmployeeDetailsRepository.GetByFiltersAsync: prioriza
        // EmployeeLaborRegime; si el empleado no tiene ningún registro activo ahí,
        // cae a EmployeeType legacy en vez de excluirlo en silencio.
        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.ved.EmployeeType == regimeId));
        }

        var grouped = await query
            .GroupBy(x => new
            {
                x.calc.EmployeeId,
                x.ved.IDCard,
                FullName = x.ved.LastName + " " + x.ved.FirstName,
                x.ved.Department,
                x.ved.ContractType
            })
            .Select(g => new FoodSubsidySummaryReportDto
            {
                EmployeeId     = g.Key.EmployeeId,
                IdCard         = g.Key.IDCard,
                FullName       = g.Key.FullName,
                DepartmentName = g.Key.Department,
                ContractType   = g.Key.ContractType,
                DaysWorked     = g.Sum(x => x.calc.FoodSubsidy)
            })
            .Where(r => r.DaysWorked > 0)
            .OrderBy(r => r.FullName)
            .ToListAsync(ct);

        return grouped.AsReadOnly();
    }
}
