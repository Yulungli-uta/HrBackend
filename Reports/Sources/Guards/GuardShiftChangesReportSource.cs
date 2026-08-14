using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Guards;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources.Guards;

/// <summary>
/// Origen de datos para el reporte de cambios de turno y reemplazos.
/// Incluye intercambios, ausencias y reemplazos con su estado de aprobación.
/// </summary>
public sealed class GuardShiftChangesReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<GuardShiftChangesReportSource> _logger;

    public ReportType ReportType => ReportType.GuardShiftChanges;

    private const string ColDate         = "work_date";
    private const string ColOriginal     = "original_guard";
    private const string ColOriginalCard = "original_id_card";
    private const string ColReplacement  = "replacement_guard";
    private const string ColGroup        = "group_name";
    private const string ColLocation     = "location_name";
    private const string ColChangeType   = "change_type";
    private const string ColReason       = "reason";
    private const string ColStatus       = "status";
    private const string ColRequestedAt  = "requested_at";
    private const string ColApprovedBy   = "approved_by";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColDate,         "Fecha turno",      Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColOriginalCard, "Cédula",           Width: 1.2f),
        new(ColOriginal,     "Guardia original", Width: 2.4f),
        new(ColReplacement,  "Reemplazante",     Width: 2.4f),
        new(ColGroup,        "Grupo",            Width: 1.6f),
        new(ColLocation,     "Ubicación",        Width: 2.0f),
        new(ColChangeType,   "Tipo cambio",      Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColReason,       "Motivo",           Width: 2.2f),
        new(ColStatus,       "Estado",           Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColRequestedAt,  "Solicitado",       Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColApprovedBy,   "Aprobado por",     Width: 1.6f),
    ];

    public GuardShiftChangesReportSource(AppDbContext db, ILogger<GuardShiftChangesReportSource> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var start = (filter.StartDate ?? DateTime.Today.AddMonths(-1)).Date;
        var end   = (filter.EndDate   ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        _logger.LogInformation(
            "Building GuardShiftChanges report. Start={Start}, End={End}, GroupId={Group}, Status={Status}",
            start, end, filter.GroupId, filter.Status);

        var query = _db.GuardShiftChanges
            .Include(c => c.Planning).ThenInclude(p => p!.Location)
            .Include(c => c.Planning).ThenInclude(p => p!.Group)
            .Include(c => c.OriginalEmployee).ThenInclude(e => e!.People)
            .Include(c => c.ReplacementEmployee).ThenInclude(e => e!.People)
            .Include(c => c.ChangeType)
            .Include(c => c.StatusType)
            .Where(c => c.RequestedAt >= start && c.RequestedAt <= end);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(c => c.StatusType!.Name == filter.Status);

        if (filter.GroupId.HasValue)
            query = query.Where(c => c.Planning!.GroupId == filter.GroupId.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(c => c.Planning!.LocationId == filter.LocationId.Value
                                   || c.Planning!.Location!.ParentLocationId == filter.LocationId.Value);

        if (filter.EmployeeId.HasValue)
            query = query.Where(c => c.OriginalEmployeeId == filter.EmployeeId.Value
                                   || c.ReplacementEmployeeId == filter.EmployeeId.Value);

        var data = await query
            .OrderBy(c => c.OriginalEmployee!.People!.LastName)
            .ThenBy(c => c.OriginalEmployee!.People!.FirstName)
            .ThenByDescending(c => c.RequestedAt)
            .ToListAsync();

        _logger.LogInformation("GuardShiftChanges report: {Count} records.", data.Count);

        var rows = data.Select(c =>
        {
            var loc = c.Planning?.Location;
            var locLabel = loc?.LocationCode != null ? $"[{loc.LocationCode}] {loc.LocationName}" : (loc?.LocationName ?? "—");
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColDate]         = c.Planning?.WorkDate.ToString("dd/MM/yyyy") ?? "—",
                [ColOriginalCard] = c.OriginalEmployee?.People?.IdCard ?? "—",
                [ColOriginal]     = c.OriginalEmployee?.People.GetFullName() ?? string.Empty,
                [ColReplacement]  = c.ReplacementEmployee != null
                                    ? c.ReplacementEmployee.People.GetFullName()
                                    : "—",
                [ColGroup]        = c.Planning?.Group?.Name ?? "—",
                [ColLocation]     = locLabel,
                [ColChangeType]   = c.ChangeType?.Name ?? "—",
                [ColReason]       = c.Reason ?? "—",
                [ColStatus]       = c.StatusType?.Name ?? "—",
                [ColRequestedAt]  = c.RequestedAt.ToString("dd/MM/yyyy HH:mm"),
                [ColApprovedBy]   = c.ApprovedBy.HasValue ? c.ApprovedBy.ToString()! : "—",
            };
        }).ToList();

        var parts = new List<string>
        {
            $"Período: {start:dd/MM/yyyy} — {end:dd/MM/yyyy}",
        };
        if (!string.IsNullOrWhiteSpace(filter.Status)) parts.Add($"Estado: {filter.Status}");
        parts.Add($"Total: {data.Count}");

        return new ReportDefinition
        {
            Title       = "Cambios de Turno y Reemplazos",
            FilePrefix  = "Reporte_Cambios_Turno_Guardias",
            Subtitle    = string.Join(" | ", parts),
            GeneratedBy = context.User.Identity?.Name ?? "sistema",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
        };
    }
}
