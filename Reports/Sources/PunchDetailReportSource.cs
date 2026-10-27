using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Reporte detallado de picadas: por persona y día, el horario programado, si tiene almuerzo, las cuatro picadas
/// (entrada, salida a almuerzo, regreso de almuerzo y salida) y si tuvo atraso (con sus minutos).
/// Incluye los días programados sin picadas. Rango máximo de 31 días.
/// </summary>
public sealed class PunchDetailReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<PunchDetailReportSource> _logger;

    /// <inheritdoc/>
    public ReportType ReportType => ReportType.PunchDetail;

    private const string ColIdCard = "id_card";
    private const string ColFullName = "full_name";
    private const string ColWorkDate = "work_date";
    private const string ColScheduledEntry = "scheduled_entry";
    private const string ColScheduledExit = "scheduled_exit";
    private const string ColLunch = "lunch";
    private const string ColEntry = "entry";
    private const string ColLunchOut = "lunch_out";
    private const string ColLunchIn = "lunch_in";
    private const string ColExit = "exit";
    private const string ColLateMinutes = "late_minutes";
    private const string ColLate = "late";

    private static readonly IReadOnlyList<ReportColumn> Columns =
    [
        new(ColIdCard,         "Cédula",           62f,  ColumnAlignment.Left),
        new(ColFullName,       "Nombres",          150f, ColumnAlignment.Left),
        new(ColWorkDate,       "Fecha",            55f,  ColumnAlignment.Center),
        new(ColScheduledEntry, "Entrada prog.",    45f,  ColumnAlignment.Center),
        new(ColScheduledExit,  "Salida prog.",     45f,  ColumnAlignment.Center),
        new(ColLunch,          "Almuerzo",         72f,  ColumnAlignment.Center),
        new(ColEntry,          "Entrada",          45f,  ColumnAlignment.Center),
        new(ColLunchOut,       "Salida almuerzo",  50f,  ColumnAlignment.Center),
        new(ColLunchIn,        "Regreso almuerzo", 50f,  ColumnAlignment.Center),
        new(ColExit,           "Salida",           45f,  ColumnAlignment.Center),
        new(ColLateMinutes,    "Min. atraso",      42f,  ColumnAlignment.Right),
        new(ColLate,           "Atraso",           40f,  ColumnAlignment.Center),
    ];

    public PunchDetailReportSource(IAttendanceCalculationsReportService service, ILogger<PunchDetailReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("[PunchDetailReportSource] Construyendo reporte detallado de picadas. Filtro: {@Filter}", filter);

        var data = await _service.GetPunchDetailDataAsync(filter, context.RequestAborted);

        static string Time(TimeOnly? t) => t?.ToString("HH:mm") ?? "—";
        static string Punch(DateTime? t) => t?.ToString("HH:mm:ss") ?? "—";

        var rows = data.Select(d => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            [ColIdCard] = d.IdCard,
            [ColFullName] = d.FullName,
            [ColWorkDate] = d.WorkDate.ToString("dd/MM/yyyy"),
            [ColScheduledEntry] = Time(d.ScheduledEntryTime),
            [ColScheduledExit] = Time(d.ScheduledExitTime),
            [ColLunch] = d.HasLunch ? $"Sí ({Time(d.ScheduledLunchStart)}-{Time(d.ScheduledLunchEnd)})" : "No",
            [ColEntry] = Punch(d.EntryPunch),
            [ColLunchOut] = d.HasLunch ? Punch(d.LunchOutPunch) : "—",
            [ColLunchIn] = d.HasLunch ? Punch(d.LunchInPunch) : "—",
            [ColExit] = Punch(d.ExitPunch),
            [ColLateMinutes] = d.LateMinutes,
            [ColLate] = d.HasLateness ? "Sí" : "No",
        }).ToList().AsReadOnly();

        _logger.LogInformation("[PunchDetailReportSource] Reporte construido. Filas: {Count}", rows.Count);

        return new ReportDefinition
        {
            Title = "Reporte detallado de picadas",
            FilePrefix = "Reporte_Detallado_Picadas",
            Subtitle = BuildSubtitle(filter, data.Count),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = Columns,
            Rows = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true,
        };
    }

    private static string BuildSubtitle(ReportFilterDto filter, int totalRows)
    {
        var parts = new List<string>();
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate.Value:dd/MM/yyyy} — {filter.EndDate.Value:dd/MM/yyyy}");
        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0) parts.Add($"Empleado ID: {filter.EmployeeId.Value}");
        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0) parts.Add($"Dependencia ID: {filter.DepartmentId.Value}");
        parts.Add($"Total registros: {totalRows}");
        return string.Join(" | ", parts);
    }
}
