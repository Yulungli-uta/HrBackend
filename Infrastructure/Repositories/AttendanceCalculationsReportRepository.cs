using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
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

        // Filtro opcional por régimen laboral — mismo criterio que GetFoodSubsidySummaryDataAsync:
        // prioriza EmployeeLaborRegime activo; si el empleado no tiene ninguno, cae a
        // EmployeeType legacy en vez de excluirlo en silencio.
        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.emp.EmployeeType == regimeId));
        }

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

        // Filtro opcional por régimen laboral — mismo criterio que GetFoodSubsidySummaryDataAsync:
        // prioriza EmployeeLaborRegime activo; si el empleado no tiene ninguno, cae a
        // EmployeeType legacy en vez de excluirlo en silencio.
        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.emp.EmployeeType == regimeId));
        }

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

        // Filtro opcional por régimen laboral — mismo criterio que GetFoodSubsidySummaryDataAsync:
        // prioriza EmployeeLaborRegime activo; si el empleado no tiene ninguno, cae a
        // EmployeeType legacy en vez de excluirlo en silencio. Antes NO se aplicaba aquí pese
        // a que el frontend anuncia este filtro (reports.ts 'attendance-cross') — bug real
        // encontrado 2026-10-06: filtrar "Código de Trabajo" seguía mostrando empleados LOSEP.
        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.emp.EmployeeType == regimeId));
        }

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

        // Filtro opcional por régimen laboral — mismo criterio que GetFoodSubsidySummaryDataAsync:
        // prioriza EmployeeLaborRegime activo; si el empleado no tiene ninguno, cae a
        // EmployeeType legacy en vez de excluirlo en silencio.
        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.emp.EmployeeType == regimeId));
        }

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

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FoodSubsidyByScheduleReportDto>> GetFoodSubsidyByScheduleDataAsync(
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
                       && calc.FoodSubsidy > 0
                    select new { calc, ved };

        // Mismos filtros opcionales que GetFoodSubsidySummaryDataAsync
        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.ved.DepartmentID == filter.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Identification))
            query = query.Where(x => x.ved.IDCard == filter.Identification);

        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.ved.EmployeeType == regimeId));
        }

        // Left join a Schedules por AppliedScheduleId — puede ser NULL si la jornada
        // no tiene horario de catálogo resuelto (caso raro para Código de Trabajo).
        // Agrupado por (empleado, horario): sin fecha individual, suma las jornadas
        // que ese empleado calificó en cada horario distinto durante el período.
        var withSchedule = from x in query
                            join sch in _db.Schedules.AsNoTracking()
                                on x.calc.AppliedScheduleId equals (int?)sch.ScheduleId into schJoin
                            from sch in schJoin.DefaultIfEmpty()
                            select new { x.calc, x.ved, sch };

        var grouped = await withSchedule
            .GroupBy(x => new
            {
                x.calc.EmployeeId,
                x.ved.IDCard,
                FullName  = x.ved.LastName + " " + x.ved.FirstName,
                ScheduleId = x.calc.AppliedScheduleId,
                EntryTime  = x.sch != null ? (TimeOnly?)x.sch.EntryTime : null,
                ExitTime   = x.sch != null ? (TimeOnly?)x.sch.ExitTime  : null
            })
            .Select(g => new FoodSubsidyByScheduleReportDto
            {
                EmployeeId    = g.Key.EmployeeId,
                IdCard        = g.Key.IDCard,
                FullName      = g.Key.FullName,
                EntryTime     = g.Key.EntryTime,
                ExitTime      = g.Key.ExitTime,
                JourneysCount = g.Sum(x => x.calc.FoodSubsidy)
            })
            .Where(r => r.JourneysCount > 0)
            .OrderBy(r => r.FullName)
            .ThenBy(r => r.EntryTime)
            .ToListAsync(ct);

        return grouped.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NightHoursSummaryReportDto>> GetNightHoursSummaryDataAsync(
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
                       && calc.NightMinutes > 0
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

        // Filtro opcional por régimen laboral. A diferencia del subsidio de
        // alimentación, la jornada nocturna no es exclusiva de un régimen — se
        // deja como filtro opcional, no como exclusión implícita.
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
                FullName     = x.ved.LastName + " " + x.ved.FirstName,
                x.ved.Department,
                x.ved.ContractType
            })
            .Select(g => new NightHoursSummaryReportDto
            {
                EmployeeId         = g.Key.EmployeeId,
                IdCard             = g.Key.IDCard,
                FullName           = g.Key.FullName,
                DepartmentName     = g.Key.Department,
                ContractType       = g.Key.ContractType,
                DaysWithNightHours = g.Count(),
                TotalNightMinutes  = g.Sum(x => x.calc.NightMinutes)
            })
            .Where(r => r.TotalNightMinutes > 0)
            .OrderBy(r => r.FullName)
            .ToListAsync(ct);

        return grouped
            .Select(r => r with { TotalNightHours = Math.Round(r.TotalNightMinutes / 60m, 2) })
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AttendanceNoveltyReportDto>> GetAttendanceNoveltiesDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var novelties = await BuildAttendanceNoveltiesAsync(filter, ct);
        return novelties.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AttendanceNoveltyReportDto>> GetAttendanceNoveltiesSummaryAsync(
        ReportFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var novelties = await BuildAttendanceNoveltiesAsync(filter, ct);

        // Búsqueda parcial por cédula o nombre — mismo criterio que GetLatenessSummaryDataAsync
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            novelties = novelties
                .Where(n => n.IdCard.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || n.FullName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.NoveltyType))
        {
            novelties = novelties.Where(n => n.NoveltyType == filter.NoveltyType).ToList();
        }

        var totalCount = novelties.Count;
        var pageItems = novelties
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AttendanceNoveltyReportDto>
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Detecta las novedades de asistencia (ausencia injustificada, picada sin captura
    /// confiable, atraso, salida anticipada, ajuste manual, horas fuera de horario,
    /// recuperación aplicada, reemplazo de guardia) para el rango/filtros indicados.
    /// Compartido por <see cref="GetAttendanceNoveltiesDataAsync"/> (para el PDF/Excel) y
    /// <see cref="GetAttendanceNoveltiesSummaryAsync"/> (para la pantalla paginada en vivo).
    /// </summary>
    private async Task<List<AttendanceNoveltyReportDto>> BuildAttendanceNoveltiesAsync(
        ReportFilterDto filter,
        CancellationToken ct)
    {
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

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.ved.DepartmentID == filter.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Identification))
            query = query.Where(x => x.ved.IDCard == filter.Identification);

        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.ved.EmployeeType == regimeId));
        }

        var rows = await query
            .Select(x => new
            {
                x.calc.EmployeeId,
                x.ved.IDCard,
                FullName = x.ved.LastName + " " + x.ved.FirstName,
                x.ved.Department,
                x.calc.WorkDate,
                x.calc.JourneyNumber,
                x.calc.TotalWorkedMinutes,
                x.calc.AbsentMinutes,
                x.calc.HasVacation,
                x.calc.HasPermission,
                x.calc.HasMedicalLeave,
                x.calc.HasJustification,
                x.calc.HasManualAdjustment,
                x.calc.TardinessMin,
                x.calc.MinutesLate,
                x.calc.EarlyLeaveMinutes,
                x.calc.OffScheduleMin,
                x.calc.RecoveredMinutes,
                x.calc.IsReplacement,
                x.calc.OriginalEmployeeId,
                x.calc.ScheduledEntryTime,
                x.calc.ScheduledExitTime
            })
            .ToListAsync(ct);

        if (rows.Count == 0) return [];

        var employeeIds = rows.Select(r => r.EmployeeId).Distinct().ToList();

        // Picadas reales (con hora exacta) por (EmployeeId, fecha calendario) en el rango —
        // +1 dia para poder incluir la salida de un turno nocturno que cae al dia siguiente.
        var startDt = startDate.ToDateTime(TimeOnly.MinValue);
        var endDt = endDate.ToDateTime(TimeOnly.MaxValue).AddDays(1);
        var rawPunches = await _db.AttendancePunches.AsNoTracking()
            .Where(p => employeeIds.Contains(p.EmployeeId) && p.PunchTime >= startDt && p.PunchTime <= endDt)
            .Select(p => new { p.EmployeeId, p.PunchTime, p.PunchType })
            .OrderBy(p => p.PunchTime)
            .ToListAsync(ct);

        var punchLookup = rawPunches
            .GroupBy(p => (p.EmployeeId, Date: DateOnly.FromDateTime(p.PunchTime.Date)))
            .ToDictionary(g => g.Key, g => g.Select(p => (p.PunchTime, p.PunchType)).ToList());

        var punchSet = new HashSet<(int EmployeeId, DateOnly Date)>(punchLookup.Keys);

        static string FormatPunches(List<(DateTime PunchTime, string PunchType)> punches)
        {
            if (punches.Count == 0) return "Sin marcaciones registradas ese día.";
            var tipoEs = new Func<string, string>(t => t.Equals("In", StringComparison.OrdinalIgnoreCase) ? "Entrada" : "Salida");
            var items = punches.Select(p => $"{tipoEs(p.PunchType)} {p.PunchTime:dd/MM/yyyy HH:mm}");
            return "Marcaciones registradas: " + string.Join(", ", items) + ".";
        }

        // Nombres del empleado original para casos de reemplazo de guardia
        var originalIds = rows
            .Where(r => r.IsReplacement && r.OriginalEmployeeId.HasValue)
            .Select(r => r.OriginalEmployeeId!.Value)
            .Distinct()
            .ToList();

        var originalNames = originalIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.vwEmployeeDetails.AsNoTracking()
                .Where(v => originalIds.Contains(v.EmployeeID))
                .ToDictionaryAsync(v => v.EmployeeID, v => v.LastName + " " + v.FirstName, ct);

        var result = new List<AttendanceNoveltyReportDto>();

        foreach (var r in rows)
        {
            // Solo se mira el día calendario siguiente cuando el horario de ESTA jornada
            // realmente cruza medianoche (ej. Noche 23:00-07:30). Para un horario de día
            // normal, mirar el día siguiente "tomaba prestadas" picadas de otra jornada ya
            // procesada aparte (caso real: 1104487424, horario 07:00-16:30).
            var crossesMidnight = r.ScheduledEntryTime.HasValue && r.ScheduledExitTime.HasValue
                && r.ScheduledExitTime.Value <= r.ScheduledEntryTime.Value;

            var hasPunchToday = punchSet.Contains((r.EmployeeId, r.WorkDate))
                || (crossesMidnight && punchSet.Contains((r.EmployeeId, r.WorkDate.AddDays(1))));

            var journeyPunches = new List<(DateTime PunchTime, string PunchType)>();
            if (punchLookup.TryGetValue((r.EmployeeId, r.WorkDate), out var todayPunches))
                journeyPunches.AddRange(todayPunches);
            if (crossesMidnight && punchLookup.TryGetValue((r.EmployeeId, r.WorkDate.AddDays(1)), out var nextDayPunches))
                journeyPunches.AddRange(nextDayPunches);
            var punchesText = FormatPunches(journeyPunches);

            void Add(string type, string label, string observation) =>
                result.Add(new AttendanceNoveltyReportDto
                {
                    EmployeeId = r.EmployeeId,
                    IdCard = r.IDCard,
                    FullName = r.FullName,
                    DepartmentName = r.Department,
                    WorkDate = r.WorkDate,
                    JourneyNumber = r.JourneyNumber,
                    NoveltyType = type,
                    NoveltyLabel = label,
                    Observation = $"{observation} {punchesText}",
                    ScheduledEntryTime = r.ScheduledEntryTime,
                    ScheduledExitTime = r.ScheduledExitTime
                });

            if (r.AbsentMinutes > 0 && !r.HasVacation && !r.HasPermission && !r.HasMedicalLeave && !r.HasJustification && !hasPunchToday)
            {
                Add("UNJUSTIFIED_ABSENCE", "Ausencia injustificada",
                    "Ausencia sin justificación registrada (permiso, vacación, licencia médica) y sin marcación biométrica.");
            }

            if (r.TotalWorkedMinutes == 0 && hasPunchToday)
            {
                Add("UNRELIABLE_PUNCH_CAPTURE", "Picada sin captura confiable",
                    "Existen marcaciones ese día pero no se pudo determinar el tiempo trabajado de forma confiable (turno extendido sin salida registrada o marcación duplicada).");
            }

            if (r.TardinessMin > 0 || r.MinutesLate > 0)
            {
                var mins = r.TardinessMin > 0 ? r.TardinessMin : r.MinutesLate;
                Add("LATE_ARRIVAL", "Atraso",
                    $"Ingreso con {mins} minutos de retraso respecto al horario asignado.");
            }

            if (r.EarlyLeaveMinutes > 0)
            {
                Add("EARLY_LEAVE", "Salida anticipada",
                    $"Salida {r.EarlyLeaveMinutes} minutos antes de lo programado.");
            }

            if (r.HasManualAdjustment)
            {
                Add("MANUAL_ADJUSTMENT", "Ajuste manual",
                    "El cálculo de este día fue corregido manualmente por un usuario.");
            }

            if (r.RecoveredMinutes > 0)
            {
                Add("TIME_RECOVERY_APPLIED", "Recuperación aplicada",
                    $"Se aplicó un plan de recuperación de tiempo por {r.RecoveredMinutes} minutos.");
            }

            if (r.IsReplacement && r.OriginalEmployeeId.HasValue)
            {
                var originalName = originalNames.TryGetValue(r.OriginalEmployeeId.Value, out var n) ? n : "desconocido";
                Add("GUARD_REPLACEMENT", "Reemplazo de guardia",
                    $"Turno cubierto por reemplazo (empleado original: {originalName}).");
            }
        }

        return result
            .OrderBy(r => r.FullName)
            .ThenBy(r => r.WorkDate)
            .ThenBy(r => r.JourneyNumber)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<LatenessSummaryReportDto>> GetLatenessSummaryDataAsync(
        ReportFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var startDate = filter.StartDate.HasValue
            ? DateOnly.FromDateTime(filter.StartDate.Value)
            : DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

        var endDate = filter.EndDate.HasValue
            ? DateOnly.FromDateTime(filter.EndDate.Value)
            : DateOnly.FromDateTime(DateTime.Today);

        var query = from calc in _db.AttendanceCalculations.AsNoTracking()
                    join ved in _db.vwEmployeeDetails.AsNoTracking()
                        on calc.EmployeeId equals ved.EmployeeID
                    where calc.WorkDate >= startDate
                       && calc.WorkDate <= endDate
                       && (calc.MinutesLate > 0 || calc.TardinessMin > 0)
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

        // Búsqueda parcial por cédula o nombre (caja de búsqueda de la pantalla de resumen)
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x =>
                x.ved.IDCard.Contains(term)
                || (x.ved.LastName + " " + x.ved.FirstName).Contains(term));
        }

        // Filtro opcional por régimen laboral — mismo criterio que GetFoodSubsidySummaryDataAsync:
        // prioriza EmployeeLaborRegime activo; si el empleado no tiene ninguno, cae a
        // EmployeeType legacy en vez de excluirlo en silencio.
        if (filter.LaborRegimeId.HasValue && filter.LaborRegimeId.Value > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.calc.EmployeeId && r.IsActive)
                    && x.ved.EmployeeType == regimeId));
        }

        var groupedQuery = query
            .GroupBy(x => new
            {
                x.calc.EmployeeId,
                x.ved.IDCard,
                FullName = x.ved.LastName + " " + x.ved.FirstName,
                x.ved.Department,
                x.ved.ContractType
            })
            .Select(g => new LatenessSummaryReportDto
            {
                EmployeeId       = g.Key.EmployeeId,
                IdCard           = g.Key.IDCard,
                FullName         = g.Key.FullName,
                DepartmentName   = g.Key.Department,
                ContractType     = g.Key.ContractType,
                LateDaysCount    = g.Count(),
                TotalMinutesLate = g.Sum(x => x.calc.TardinessMin)
            });

        var totalCount = await groupedQuery.LongCountAsync(ct);

        var pageItems = await groupedQuery
            .OrderByDescending(r => r.LateDaysCount)
            .ThenBy(r => r.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<LatenessSummaryReportDto>
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>Fila cruda intermedia para el reporte de planificación vs. ejecución de horas
    /// extra — compartida entre el detalle diario y el resumen mensual para no duplicar el join.</summary>
    private sealed record OvertimePlanRawRow(
        int EmployeeId,
        string IdCard,
        string FullName,
        int? DepartmentId,
        int PlanId,
        string PlanTitle,
        DateTime PlanStartDate,
        DateTime PlanEndDate,
        decimal? AssignedHours,
        int EmployeeStatusTypeId,
        DateTime WorkDate,
        int TotalMinutes,
        int OvertimeMinutes,
        int NightMinutes,
        int HolidayMinutes);

    private async Task<List<OvertimePlanRawRow>> GetOvertimePlanRawRowsAsync(
        ReportFilterDto filter,
        CancellationToken ct,
        bool applyDateFilter = true)
    {
        var query = from tex in _db.TimePlanningExecution.AsNoTracking()
                    join tpe in _db.TimePlanningEmployee.AsNoTracking() on tex.PlanEmployeeID equals tpe.PlanEmployeeID
                    join tp in _db.TimePlanning.AsNoTracking() on tpe.PlanID equals tp.PlanID
                    join emp in _db.Employees.AsNoTracking() on tpe.EmployeeID equals emp.EmployeeId
                    join person in _db.People.AsNoTracking() on emp.PersonID equals person.PersonId
                    where tp.PlanType == "Overtime"
                    select new { tex, tpe, tp, emp, person };

        // El resumen mensual no filtra por fecha (quiere ver todo el historial agregado por
        // mes, sin que el usuario tenga que elegir un rango); el detalle diario sí lo respeta.
        if (applyDateFilter)
        {
            var startDate = filter.StartDate ?? DateTime.Today.AddMonths(-1);
            var endDate = filter.EndDate ?? DateTime.Today;
            query = query.Where(x => x.tex.WorkDate.Date >= startDate.Date && x.tex.WorkDate.Date <= endDate.Date);
        }

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.tpe.EmployeeID == filter.EmployeeId.Value);

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            query = query.Where(x => x.emp.DepartmentId == filter.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Identification))
            query = query.Where(x => x.person.IdCard == filter.Identification);

        return await query
            .Select(x => new OvertimePlanRawRow(
                x.tpe.EmployeeID,
                x.person.IdCard,
                x.person.LastName + " " + x.person.FirstName,
                x.emp.DepartmentId,
                x.tp.PlanID,
                x.tp.Title,
                x.tp.StartDate,
                x.tp.EndDate,
                x.tpe.AssignedHours,
                x.tpe.EmployeeStatusTypeID,
                x.tex.WorkDate,
                x.tex.TotalMinutes,
                x.tex.OvertimeMinutes,
                x.tex.NightMinutes,
                x.tex.HolidayMinutes))
            .ToListAsync(ct);
    }

    private async Task<(
        Dictionary<int, string> Departments,
        Dictionary<int, string> StatusNames,
        Dictionary<(int EmployeeId, DateOnly WorkDate), string> OvertimeStatuses)>
        ResolveOvertimePlanLookupsAsync(List<OvertimePlanRawRow> rows, bool includeOvertimeStatus, CancellationToken ct)
    {
        var deptIds = rows.Where(r => r.DepartmentId.HasValue).Select(r => r.DepartmentId!.Value).Distinct().ToList();
        var departments = await _db.Departments.AsNoTracking()
            .Where(d => deptIds.Contains(d.DepartmentId))
            .ToDictionaryAsync(d => d.DepartmentId, d => d.Name, ct);

        var statusIds = rows.Select(r => r.EmployeeStatusTypeId).Distinct().ToList();
        var statusNames = await _db.RefTypes.AsNoTracking()
            .Where(rt => rt.Category == "EMPLOYEE_PLAN_STATUS" && statusIds.Contains(rt.TypeId))
            .ToDictionaryAsync(rt => rt.TypeId, rt => rt.Name, ct);

        var overtimeStatuses = new Dictionary<(int, DateOnly), string>();
        if (includeOvertimeStatus)
        {
            var employeeIds = rows.Select(r => r.EmployeeId).Distinct().ToList();
            var workDates = rows.Select(r => DateOnly.FromDateTime(r.WorkDate)).Distinct().ToList();

            var otRows = await _db.Overtime.AsNoTracking()
                .Where(o => employeeIds.Contains(o.EmployeeId) && workDates.Contains(o.WorkDate))
                .Select(o => new { o.EmployeeId, o.WorkDate, o.Status })
                .ToListAsync(ct);

            foreach (var o in otRows)
                overtimeStatuses[(o.EmployeeId, o.WorkDate)] = o.Status;
        }

        return (departments, statusNames, overtimeStatuses);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OvertimePlanDailyReportDto>> GetOvertimePlanDailyDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var rows = await GetOvertimePlanRawRowsAsync(filter, ct);
        if (rows.Count == 0) return Array.Empty<OvertimePlanDailyReportDto>();

        var (departments, statusNames, overtimeStatuses) = await ResolveOvertimePlanLookupsAsync(rows, includeOvertimeStatus: true, ct);

        return rows
            .Select(r => new OvertimePlanDailyReportDto
            {
                EmployeeId = r.EmployeeId,
                IdCard = r.IdCard,
                FullName = r.FullName,
                DepartmentName = r.DepartmentId.HasValue && departments.TryGetValue(r.DepartmentId.Value, out var dn) ? dn : null,
                PlanId = r.PlanId,
                PlanTitle = r.PlanTitle,
                PlanStartDate = r.PlanStartDate,
                PlanEndDate = r.PlanEndDate,
                AssignedHours = r.AssignedHours,
                WorkDate = DateOnly.FromDateTime(r.WorkDate),
                ExecutedTotalMinutes = r.TotalMinutes,
                ExecutedOvertimeMinutes = r.OvertimeMinutes,
                ExecutedNightMinutes = r.NightMinutes,
                ExecutedHolidayMinutes = r.HolidayMinutes,
                EmployeeStatusName = statusNames.TryGetValue(r.EmployeeStatusTypeId, out var sn) ? sn : "Desconocido",
                OvertimeStatus = overtimeStatuses.TryGetValue((r.EmployeeId, DateOnly.FromDateTime(r.WorkDate)), out var os) ? os : null
            })
            .OrderBy(r => r.FullName).ThenBy(r => r.WorkDate)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OvertimePlanMonthlyReportDto>> GetOvertimePlanMonthlyDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Sin filtro de rango startDate/endDate a propósito: el resumen mensual filtra por
        // Año/Mes (más natural para un reporte "por mes"), no por un rango de fechas.
        var rows = await GetOvertimePlanRawRowsAsync(filter, ct, applyDateFilter: false);

        if (filter.Year.HasValue)
            rows = rows.Where(r => r.WorkDate.Year == filter.Year.Value).ToList();

        if (filter.Month.HasValue)
            rows = rows.Where(r => r.WorkDate.Month == filter.Month.Value).ToList();

        if (rows.Count == 0) return Array.Empty<OvertimePlanMonthlyReportDto>();

        var deptIds = rows.Where(r => r.DepartmentId.HasValue).Select(r => r.DepartmentId!.Value).Distinct().ToList();
        var departments = await _db.Departments.AsNoTracking()
            .Where(d => deptIds.Contains(d.DepartmentId))
            .ToDictionaryAsync(d => d.DepartmentId, d => d.Name, ct);

        return rows
            // Agrupa por EMPLEADO+MES, no por plan: una persona con 2+ planificaciones en el
            // mismo mes debe aparecer en una sola fila con el total sumado de todas.
            .GroupBy(r => (r.EmployeeId, Period: r.WorkDate.ToString("yyyy-MM")))
            .Select(g =>
            {
                var first = g.First();
                // Cada plan distinto se cuenta una sola vez (no una vez por día ejecutado),
                // para no inflar "Horas Planificadas" cuando el mismo plan tiene varios días.
                var distinctPlans = g.Select(x => new { x.PlanId, x.AssignedHours }).Distinct().ToList();

                return new OvertimePlanMonthlyReportDto
                {
                    EmployeeId = first.EmployeeId,
                    IdCard = first.IdCard,
                    FullName = first.FullName,
                    DepartmentName = first.DepartmentId.HasValue && departments.TryGetValue(first.DepartmentId.Value, out var dn) ? dn : null,
                    AssignedHours = distinctPlans.Sum(p => p.AssignedHours ?? 0),
                    Period = g.Key.Period,
                    PlanCount = distinctPlans.Count,
                    ExecutedTotalMinutes = g.Sum(x => x.TotalMinutes),
                    ExecutedOvertimeMinutes = g.Sum(x => x.OvertimeMinutes),
                    ExecutedNightMinutes = g.Sum(x => x.NightMinutes),
                    ExecutedHolidayMinutes = g.Sum(x => x.HolidayMinutes),
                };
            })
            .OrderBy(r => r.FullName).ThenBy(r => r.Period)
            .ToList()
            .AsReadOnly();
    }
}
