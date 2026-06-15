using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources.Guards;

/// <summary>
/// Origen de datos para el reporte de cobertura por ubicación.
/// Para cada combinación ubicación+fecha+turno indica cuántos guardias están asignados
/// y si la cobertura es suficiente (al menos 1 guardia activo).
/// </summary>
public sealed class GuardLocationCoverageReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<GuardLocationCoverageReportSource> _logger;

    public ReportType ReportType => ReportType.GuardLocationCoverage;

    private const string ColCampus    = "campus";
    private const string ColLocation  = "location";
    private const string ColDate      = "work_date";
    private const string ColShift     = "shift_code";
    private const string ColCount     = "guard_count";
    private const string ColCoverage  = "coverage_status";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColCampus,   "Campus / Raíz",    Width: 2.0f),
        new(ColLocation, "Ubicación",        Width: 2.8f),
        new(ColDate,     "Fecha",            Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColShift,    "Turno",            Width: 0.9f, Alignment: ColumnAlignment.Center),
        new(ColCount,    "Guardias",         Width: 0.9f, Alignment: ColumnAlignment.Right),
        new(ColCoverage, "Cobertura",        Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public GuardLocationCoverageReportSource(AppDbContext db, ILogger<GuardLocationCoverageReportSource> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var start = filter.StartDate.HasValue ? DateOnly.FromDateTime(filter.StartDate.Value) : DateOnly.FromDateTime(DateTime.Today);
        var end   = filter.EndDate.HasValue   ? DateOnly.FromDateTime(filter.EndDate.Value)   : start.AddDays(6);

        _logger.LogInformation(
            "Building GuardLocationCoverage report. Start={Start}, End={End}, LocationId={Loc}",
            start, end, filter.LocationId);

        var query = _db.GuardShiftPlannings
            .Include(p => p.Location).ThenInclude(l => l!.Parent)
            .Include(p => p.Schedule)
            .Where(p => p.IsActiveForAssignment
                     && p.WorkDate >= start
                     && p.WorkDate <= end);

        if (filter.LocationId.HasValue)
            query = query.Where(p => p.LocationId == filter.LocationId.Value
                                  || p.Location!.ParentLocationId == filter.LocationId.Value
                                  || p.Location!.RootLocationId == filter.LocationId.Value);

        var plannings = await query.ToListAsync();

        // Agrupa por (locationId, locationName, parentName, workDate, scheduleCode)
        var grouped = plannings
            .GroupBy(p => new
            {
                p.LocationId,
                LocationName = p.Location?.LocationCode != null
                               ? $"[{p.Location.LocationCode}] {p.Location.LocationName}"
                               : (p.Location?.LocationName ?? "—"),
                ParentName = p.Location?.Parent?.LocationName ?? p.Location?.LocationName ?? "—",
                p.WorkDate,
                ShiftCode = p.Schedule?.ScheduleCode ?? "?",
            })
            .OrderBy(g => g.Key.ParentName)
            .ThenBy(g => g.Key.LocationName)
            .ThenBy(g => g.Key.WorkDate)
            .ThenBy(g => g.Key.ShiftCode);

        var rows = grouped.Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            [ColCampus]   = g.Key.ParentName,
            [ColLocation] = g.Key.LocationName,
            [ColDate]     = g.Key.WorkDate.ToString("dd/MM/yyyy"),
            [ColShift]    = g.Key.ShiftCode,
            [ColCount]    = g.Count(),
            [ColCoverage] = g.Count() > 0 ? "Con cobertura" : "Sin cobertura",
        }).ToList();

        // Agrega filas de "Sin cobertura" para ubicaciones con RequiresCoverage y sin planificación
        var coveredKeys = new HashSet<(int locId, DateOnly date, string shift)>(
            grouped.Select(g => (g.Key.LocationId, g.Key.WorkDate, g.Key.ShiftCode)));

        var requiredLocations = await _db.Set<WsUtaSystem.Models.Guards.GuardServiceLocation>()
            .Include(l => l.Parent)
            .Where(l => l.RequiresCoverage && l.IsActive)
            .ToListAsync();

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            foreach (var loc in requiredLocations)
            {
                foreach (var shift in new[] { "M", "T", "N" })
                {
                    if (!coveredKeys.Contains((loc.LocationId, d, shift)))
                    {
                        rows.Add(new Dictionary<string, object?>
                        {
                            [ColCampus]   = loc.Parent?.LocationName ?? loc.LocationName,
                            [ColLocation] = loc.LocationCode != null ? $"[{loc.LocationCode}] {loc.LocationName}" : loc.LocationName,
                            [ColDate]     = d.ToString("dd/MM/yyyy"),
                            [ColShift]    = shift,
                            [ColCount]    = 0,
                            [ColCoverage] = "Sin cobertura",
                        });
                    }
                }
            }
        }

        // Reordena incluyendo las filas sin cobertura
        var sortedRows = rows
            .OrderBy(r => r[ColCampus]?.ToString())
            .ThenBy(r => r[ColLocation]?.ToString())
            .ThenBy(r => r[ColDate]?.ToString())
            .ThenBy(r => r[ColShift]?.ToString())
            .ToList();

        _logger.LogInformation("GuardLocationCoverage report: {Count} rows.", sortedRows.Count);

        return new ReportDefinition
        {
            Title       = "Cobertura de Guardias por Ubicación",
            FilePrefix  = "Reporte_Cobertura_Guardias",
            Subtitle    = $"Período: {start:dd/MM/yyyy} — {end:dd/MM/yyyy} | Total filas: {sortedRows.Count}",
            GeneratedBy = context.User.Identity?.Name ?? "sistema",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = sortedRows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
        };
    }
}
