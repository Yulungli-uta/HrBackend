using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>Reporte detallado de picadas (una fila por jornada con sus cuatro picadas y el atraso).</summary>
public sealed partial class AttendanceCalculationsReportRepository
{
    private const string PunchTypeIn = "In";
    private const string PunchTypeOut = "Out";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PunchDetailReportDto>> GetPunchDetailDataAsync(
        ReportFilterDto filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var scope = await ResolveEmployeeScopeFromViewAsync(filter, ct);
        var rows = await PunchDetailRowsQuery(filter, scope).ToListAsync(ct);
        return await AttachPunchesAsync(rows, ct);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PunchDetailReportDto>> GetPunchDetailSummaryAsync(
        ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var scope = await ResolveEmployeeScopeFromViewAsync(filter, ct);
        var query = PunchDetailRowsQuery(filter, scope);
        var total = await query.LongCountAsync(ct);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<PunchDetailReportDto>
        {
            Items = await AttachPunchesAsync(rows, ct),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PunchDetailReportDto>> GetLatenessDetailAsync(
        ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var scope = await ResolveEmployeeScopeFromViewAsync(filter, ct);
        var query = PunchDetailRowsQuery(filter, scope, onlyLate: true);
        var total = await query.LongCountAsync(ct);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<PunchDetailReportDto>
        {
            Items = await AttachPunchesAsync(rows, ct),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    /// <summary>Fila intermedia: datos del cálculo diario antes de asignarle sus picadas.</summary>
    private sealed record PunchDetailRow(
        int EmployeeId, string IdCard, string FullName, string? DepartmentName, DateOnly WorkDate, int JourneyNumber,
        TimeOnly? ScheduledEntryTime, TimeOnly? ScheduledExitTime, bool HasLunch, TimeOnly? ScheduledLunchStart, TimeOnly? ScheduledLunchEnd,
        DateTime? FirstPunchIn, DateTime? LastPunchOut, int MinutesLate, int TardinessMin);

    /// <summary>
    /// Jornadas calculadas del rango, ordenadas por nombre y fecha. Incluye los días con horario programado
    /// aunque no tengan ninguna picada (ausencias) y los días con picadas aunque no tengan horario.
    /// <paramref name="employeeScope"/> son los empleados que cumplen dependencia y régimen
    /// (<see cref="ResolveEmployeeScopeFromViewAsync"/>); null = sin ese filtro.
    /// </summary>
    private IQueryable<PunchDetailRow> PunchDetailRowsQuery(ReportFilterDto filter, IReadOnlyCollection<int>? employeeScope, bool onlyLate = false)
    {
        var startDate = filter.StartDate.HasValue ? DateOnly.FromDateTime(filter.StartDate.Value) : _clock.Today.AddDays(-7);
        var endDate = filter.EndDate.HasValue ? DateOnly.FromDateTime(filter.EndDate.Value) : _clock.Today;

        var query = from calc in _db.AttendanceCalculations.AsNoTracking()
                    join ved in _db.vwEmployeeDetails.AsNoTracking() on calc.EmployeeId equals ved.EmployeeID
                    where calc.WorkDate >= startDate && calc.WorkDate <= endDate
                          && (calc.ScheduledEntryTime != null || calc.FirstPunchIn != null)
                    select new { calc, ved };

        // Mismo criterio de atraso que el resumen (GetLatenessSummaryDataAsync) para que el detalle cuadre con sus totales.
        if (onlyLate || filter.HasLateness == true)
            query = query.Where(x => x.calc.MinutesLate > 0 || x.calc.TardinessMin > 0);
        else if (filter.HasLateness == false)
            query = query.Where(x => x.calc.MinutesLate <= 0 && x.calc.TardinessMin <= 0);

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            query = query.Where(x => x.calc.EmployeeId == filter.EmployeeId.Value);

        if (employeeScope is not null)
            query = query.Where(x => employeeScope.Contains(x.calc.EmployeeId));

        if (!string.IsNullOrWhiteSpace(filter.Identification))
            query = query.Where(x => x.ved.IDCard == filter.Identification);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var like = $"%{filter.SearchText.Trim()}%";
            query = query.Where(x => EF.Functions.Like(x.ved.IDCard, like)
                                     || EF.Functions.Like(x.ved.LastName + " " + x.ved.FirstName, like));
        }

        return query
            .OrderBy(x => x.ved.LastName).ThenBy(x => x.ved.FirstName).ThenBy(x => x.calc.EmployeeId)
            .ThenBy(x => x.calc.WorkDate).ThenBy(x => x.calc.JourneyNumber)
            .Select(x => new PunchDetailRow(
                x.calc.EmployeeId, x.ved.IDCard, x.ved.LastName + " " + x.ved.FirstName, x.ved.Department, x.calc.WorkDate, x.calc.JourneyNumber,
                x.calc.ScheduledEntryTime, x.calc.ScheduledExitTime, x.calc.ScheduledHasLunchBreak, x.calc.ScheduledLunchStart, x.calc.ScheduledLunchEnd,
                x.calc.FirstPunchIn, x.calc.LastPunchOut, x.calc.MinutesLate, x.calc.TardinessMin));
    }

    /// <summary>
    /// Asigna a cada jornada sus cuatro picadas. Las picadas de la jornada son las que caen entre la primera
    /// entrada y la última salida del cálculo (así una jornada nocturna o un turno doble no se mezclan con otra).
    /// Se leen todas de una sola vez para los empleados del lote.
    /// </summary>
    private async Task<IReadOnlyList<PunchDetailReportDto>> AttachPunchesAsync(List<PunchDetailRow> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return [];

        var employeeIds = rows.Select(r => r.EmployeeId).Distinct().ToList();
        var from = rows.Min(r => r.FirstPunchIn ?? r.WorkDate.ToDateTime(TimeOnly.MinValue));
        var to = rows.Max(r => r.LastPunchOut ?? r.FirstPunchIn ?? r.WorkDate.ToDateTime(TimeOnly.MaxValue));

        var punches = await _db.AttendancePunches.AsNoTracking()
            .Where(p => employeeIds.Contains(p.EmployeeId) && p.PunchTime >= from && p.PunchTime <= to)
            .Select(p => new { p.EmployeeId, p.PunchTime, p.PunchType })
            .OrderBy(p => p.PunchTime)
            .ToListAsync(ct);
        var byEmployee = punches.GroupBy(p => p.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<PunchDetailReportDto>(rows.Count);
        foreach (var r in rows)
        {
            DateTime? entry = null, lunchOut = null, lunchIn = null, exit = null;
            var count = 0;

            if (r.FirstPunchIn.HasValue && byEmployee.TryGetValue(r.EmployeeId, out var all))
                (entry, lunchOut, lunchIn, exit, count) = ResolveFourPunches(all.Select(p => (p.PunchTime, (string?)p.PunchType)), r.FirstPunchIn.Value, r.LastPunchOut);

            var late = Math.Max(r.MinutesLate, r.TardinessMin);
            result.Add(new PunchDetailReportDto
            {
                EmployeeId = r.EmployeeId, IdCard = r.IdCard, FullName = r.FullName, DepartmentName = r.DepartmentName,
                WorkDate = r.WorkDate, JourneyNumber = r.JourneyNumber,
                ScheduledEntryTime = r.ScheduledEntryTime, ScheduledExitTime = r.ScheduledExitTime,
                HasLunch = r.HasLunch, ScheduledLunchStart = r.ScheduledLunchStart, ScheduledLunchEnd = r.ScheduledLunchEnd,
                EntryPunch = entry, LunchOutPunch = lunchOut, LunchInPunch = lunchIn, ExitPunch = exit, PunchCount = count,
                LateMinutes = late, HasLateness = late > 0,
            });
        }

        return result;
    }

    /// <summary>
    /// Resuelve las cuatro picadas de una jornada a partir de las picadas del empleado: sólo cuentan las que caen
    /// entre la primera entrada y la última salida del cálculo. Entrada = primera "In"; salida = última "Out" posterior;
    /// salida a almuerzo = primera "Out" intermedia; regreso = primera "In" posterior a esa salida.
    /// </summary>
    internal static (DateTime? Entry, DateTime? LunchOut, DateTime? LunchIn, DateTime? Exit, int Count) ResolveFourPunches(
        IEnumerable<(DateTime PunchTime, string? PunchType)> punches, DateTime firstPunchIn, DateTime? lastPunchOut)
    {
        var end = lastPunchOut ?? firstPunchIn;
        var window = punches.Where(p => p.PunchTime >= firstPunchIn && p.PunchTime <= end).OrderBy(p => p.PunchTime).ToList();

        var ins = window.Where(p => IsType(p.PunchType, PunchTypeIn)).ToList();
        DateTime? entry = ins.Count > 0 ? ins[0].PunchTime : null;
        var outs = window.Where(p => IsType(p.PunchType, PunchTypeOut) && (entry == null || p.PunchTime > entry)).ToList();
        DateTime? exit = outs.Count > 0 ? outs[^1].PunchTime : null;

        var middle = window.Where(p => entry != null && p.PunchTime > entry && (exit == null || p.PunchTime < exit)).ToList();
        var lunchOuts = middle.Where(p => IsType(p.PunchType, PunchTypeOut)).ToList();
        DateTime? lunchOut = lunchOuts.Count > 0 ? lunchOuts[0].PunchTime : null;
        var lunchIns = lunchOut == null ? [] : middle.Where(p => IsType(p.PunchType, PunchTypeIn) && p.PunchTime > lunchOut).ToList();
        DateTime? lunchIn = lunchIns.Count > 0 ? lunchIns[0].PunchTime : null;

        return (entry, lunchOut, lunchIn, exit, window.Count);
    }

    private static bool IsType(string? value, string expected) => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
