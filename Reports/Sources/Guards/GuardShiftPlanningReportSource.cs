using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources.Guards;

/// <summary>
/// Origen de datos para el reporte de planificación de turnos de guardias.
/// Detalla cada turno planificado: guardia, fecha, ubicación, grupo, turno y estado.
/// </summary>
public sealed class GuardShiftPlanningReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<GuardShiftPlanningReportSource> _logger;

    public ReportType ReportType => ReportType.GuardShiftPlanning;

    private const string ColIdCard      = "id_card";
    private const string ColGuard       = "guard_name";
    private const string ColGroup       = "group_name";
    private const string ColLocation    = "location_name";
    private const string ColDate        = "work_date";
    private const string ColShift       = "shift_code";
    private const string ColStatus      = "status";
    private const string ColAutoGen     = "auto_generated";
    private const string ColReplacement = "has_replacement";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColIdCard,      "Cédula",           Width: 1.4f),
        new(ColGuard,       "Guardia",          Width: 2.8f),
        new(ColGroup,       "Grupo",            Width: 1.8f),
        new(ColLocation,    "Ubicación",        Width: 2.4f),
        new(ColDate,        "Fecha",            Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColShift,       "Turno",            Width: 0.9f, Alignment: ColumnAlignment.Center),
        new(ColStatus,      "Estado",           Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColAutoGen,     "Auto-generado",    Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColReplacement, "Reemplazo",        Width: 1.0f, Alignment: ColumnAlignment.Center),
    ];

    public GuardShiftPlanningReportSource(AppDbContext db, ILogger<GuardShiftPlanningReportSource> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var start = filter.StartDate.HasValue ? DateOnly.FromDateTime(filter.StartDate.Value) : DateOnly.MinValue;
        var end   = filter.EndDate.HasValue   ? DateOnly.FromDateTime(filter.EndDate.Value)   : DateOnly.MaxValue;

        _logger.LogInformation(
            "Building GuardShiftPlanning report. Start={Start}, End={End}, LocationId={Loc}, GroupId={Group}, Status={Status}",
            start, end, filter.LocationId, filter.GroupId, filter.Status);

        var query = _db.GuardShiftPlannings
            .Include(p => p.Employee).ThenInclude(e => e!.People)
            .Include(p => p.Location)
            .Include(p => p.Group)
            .Include(p => p.Schedule)
            .Include(p => p.StatusType)
            .Include(p => p.Changes.Where(c => c.IsActiveForAttendance))
            .Where(p => p.IsActiveForAssignment
                     && p.WorkDate >= start
                     && p.WorkDate <= end);

        if (filter.LocationId.HasValue)
            query = query.Where(p => p.LocationId == filter.LocationId.Value
                                  || p.Location!.ParentLocationId == filter.LocationId.Value);

        if (filter.GroupId.HasValue)
            query = query.Where(p => p.GroupId == filter.GroupId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(p => p.StatusType!.Name == filter.Status);

        if (filter.EmployeeId.HasValue)
            query = query.Where(p => p.EmployeeId == filter.EmployeeId.Value);

        var data = await query
            .OrderBy(p => p.WorkDate)
            .ThenBy(p => p.Location!.LocationName)
            .ThenBy(p => p.Employee!.People!.LastName)
            .ToListAsync();

        _logger.LogInformation("GuardShiftPlanning report: {Count} records.", data.Count);

        var rows = data.Select(p => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            [ColIdCard]      = p.Employee?.People?.IdCard ?? "",
            [ColGuard]       = $"{p.Employee?.People?.FirstName} {p.Employee?.People?.LastName}".Trim(),
            [ColGroup]       = p.Group?.Name ?? "—",
            [ColLocation]    = p.Location?.LocationCode != null
                               ? $"[{p.Location.LocationCode}] {p.Location.LocationName}"
                               : (p.Location?.LocationName ?? "—"),
            [ColDate]        = p.WorkDate.ToString("dd/MM/yyyy"),
            [ColShift]       = p.Schedule?.ScheduleCode ?? "—",
            [ColStatus]      = p.StatusType?.Name ?? "—",
            [ColAutoGen]     = p.IsAutoGenerated ? "Sí" : "No",
            [ColReplacement] = p.Changes.Any() ? "Sí" : "No",
        }).ToList();

        var subtitle = BuildSubtitle(filter, start, end, data.Count);

        return new ReportDefinition
        {
            Title       = "Planificación de Guardias",
            FilePrefix  = "Reporte_Planificacion_Guardias",
            Subtitle    = subtitle,
            GeneratedBy = context.User.Identity?.Name ?? "sistema",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
        };
    }

    private static string BuildSubtitle(ReportFilterDto filter, DateOnly start, DateOnly end, int count)
    {
        var parts = new List<string>();
        if (start != DateOnly.MinValue || end != DateOnly.MaxValue)
            parts.Add($"Período: {start:dd/MM/yyyy} — {end:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            parts.Add($"Estado: {filter.Status}");
        parts.Add($"Total registros: {count}");
        return string.Join(" | ", parts);
    }
}
