using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources.Guards;

/// <summary>
/// Origen de datos para el reporte de guardias por grupo y su ubicación asignada.
/// Muestra cada guardia activo, el grupo al que pertenece y la sub-ubicación
/// que tiene asignada en el periodo de rotación activo (o el más reciente).
/// </summary>
public sealed class GuardGroupRosterReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<GuardGroupRosterReportSource> _logger;

    public ReportType ReportType => ReportType.GuardGroupRoster;

    private const string ColGroup        = "group_name";
    private const string ColIdCard       = "id_card";
    private const string ColGuard        = "guard_name";
    private const string ColGroupLoc     = "group_location";
    private const string ColAssignedLoc  = "assigned_location";
    private const string ColPeriod       = "period_name";
    private const string ColValidFrom    = "valid_from";
    private const string ColValidTo      = "valid_to";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColGroup,       "Grupo",                    Width: 1.8f),
        new(ColIdCard,      "Cédula",                   Width: 1.4f),
        new(ColGuard,       "Guardia",                  Width: 2.8f),
        new(ColGroupLoc,    "Ubic. del grupo",          Width: 2.0f),
        new(ColAssignedLoc, "Sub-ubic. individual",     Width: 2.2f),
        new(ColPeriod,      "Periodo rotación",         Width: 1.8f),
        new(ColValidFrom,   "En grupo desde",           Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColValidTo,     "En grupo hasta",           Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public GuardGroupRosterReportSource(AppDbContext db, ILogger<GuardGroupRosterReportSource> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building GuardGroupRoster report. GroupId={Group}, LocationId={Loc}",
            filter.GroupId, filter.LocationId);

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Periodo de rotación activo (o el más reciente)
        var activePeriod = await _db.GuardLocationRotationPeriods
            .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        activePeriod ??= await _db.GuardLocationRotationPeriods
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        // Asignaciones individuales de empleados en el periodo activo
        var empAssignments = activePeriod != null
            ? await _db.Set<WsUtaSystem.Models.Guards.GuardLocationRotationAssignment>()
                .Include(a => a.Location)
                .Where(a => a.LocationRotationPeriodId == activePeriod.LocationRotationPeriodId
                         && a.EmployeeId != null
                         && a.IsActive)
                .ToListAsync()
            : [];

        var empAssignmentLookup = empAssignments
            .GroupBy(a => a.EmployeeId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // Asignaciones de grupo a ubicación en el periodo activo
        var groupAssignments = activePeriod != null
            ? await _db.Set<WsUtaSystem.Models.Guards.GuardLocationRotationAssignment>()
                .Include(a => a.Location)
                .Where(a => a.LocationRotationPeriodId == activePeriod.LocationRotationPeriodId
                         && a.GroupId != null
                         && a.IsActive)
                .ToListAsync()
            : [];

        var groupLocationLookup = groupAssignments
            .GroupBy(a => a.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // Empleados activos en grupos
        var empQuery = _db.GuardRotationGroupEmployees
            .Include(ge => ge.Employee).ThenInclude(e => e!.People)
            .Include(ge => ge.Group)
            .Where(ge => ge.IsActive);

        if (filter.GroupId.HasValue)
            empQuery = empQuery.Where(ge => ge.GroupId == filter.GroupId.Value);

        var groupEmployees = await empQuery
            .OrderBy(ge => ge.Group!.Name)
            .ThenBy(ge => ge.Employee!.People!.LastName)
            .ToListAsync();

        // Filtro por ubicación: solo empleados cuyo grupo tiene esa ubicación o tienen asignación individual a ella
        if (filter.LocationId.HasValue)
        {
            groupEmployees = groupEmployees.Where(ge =>
            {
                if (empAssignmentLookup.TryGetValue(ge.EmployeeId, out var ea))
                    return ea.LocationId == filter.LocationId.Value
                        || ea.Location?.ParentLocationId == filter.LocationId.Value;
                if (groupLocationLookup.TryGetValue(ge.GroupId, out var ga))
                    return ga.LocationId == filter.LocationId.Value
                        || ga.Location?.ParentLocationId == filter.LocationId.Value;
                return false;
            }).ToList();
        }

        _logger.LogInformation("GuardGroupRoster report: {Count} records.", groupEmployees.Count);

        var rows = groupEmployees.Select(ge =>
        {
            groupLocationLookup.TryGetValue(ge.GroupId, out var groupLoc);
            empAssignmentLookup.TryGetValue(ge.EmployeeId, out var empLoc);

            var groupLocLabel = groupLoc?.Location != null
                ? (groupLoc.Location.LocationCode != null ? $"[{groupLoc.Location.LocationCode}] {groupLoc.Location.LocationName}" : groupLoc.Location.LocationName)
                : "No asignada";

            var empLocLabel = empLoc?.Location != null
                ? (empLoc.Location.LocationCode != null ? $"[{empLoc.Location.LocationCode}] {empLoc.Location.LocationName}" : empLoc.Location.LocationName)
                : "—";

            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColGroup]       = ge.Group?.Name ?? "—",
                [ColIdCard]      = ge.Employee?.People?.IdCard ?? "—",
                [ColGuard]       = $"{ge.Employee?.People?.FirstName} {ge.Employee?.People?.LastName}".Trim(),
                [ColGroupLoc]    = groupLocLabel,
                [ColAssignedLoc] = empLocLabel,
                [ColPeriod]      = activePeriod?.Name ?? "Sin periodo activo",
                [ColValidFrom]   = ge.ValidFrom.ToString("dd/MM/yyyy"),
                [ColValidTo]     = ge.ValidTo.HasValue ? ge.ValidTo.Value.ToString("dd/MM/yyyy") : "Vigente",
            };
        }).ToList();

        return new ReportDefinition
        {
            Title       = "Guardias por Grupo y Ubicación",
            FilePrefix  = "Reporte_Guardias_Grupo_Ubicacion",
            Subtitle    = $"Periodo: {activePeriod?.Name ?? "N/A"} | Total guardias: {rows.Count}",
            GeneratedBy = context.User.Identity?.Name ?? "sistema",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
        };
    }
}
