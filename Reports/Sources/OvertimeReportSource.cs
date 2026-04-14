using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de horas extras.
/// Consulta <c>HR.tbl_AttendanceCalculations</c> a través de
/// <see cref="IAttendanceCalculationsReportService"/> y construye la
/// <see cref="ReportDefinition"/> con las columnas y filas del reporte.
/// </summary>
/// <remarks>
/// Incluye minutos de horas extras ordinarias, nocturnas, feriado y fuera de horario
/// para dar una visión completa del tiempo adicional trabajado.
/// La orientación por defecto es <c>Landscape</c> por el alto número de columnas.
/// </remarks>
public sealed class OvertimeReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<OvertimeReportSource> _logger;

    /// <inheritdoc/>
    public ReportType ReportType => ReportType.Overtime;

    // ─── Claves de columna ────────────────────────────────────────────────────
    private const string ColEmployeeId        = "employee_id";
    private const string ColIdCard            = "id_card";
    private const string ColFullName          = "full_name";
    private const string ColWorkDate          = "work_date";
    private const string ColFirstPunchIn      = "first_punch_in";
    private const string ColLastPunchOut      = "last_punch_out";
    private const string ColScheduledEntry    = "scheduled_entry";
    private const string ColScheduledExit     = "scheduled_exit";
    private const string ColScheduledMin      = "scheduled_min";
    private const string ColTotalWorked       = "total_worked_min";
    private const string ColRegularMin        = "regular_min";
    private const string ColOvertimeMin       = "overtime_min";
    private const string ColNightMin          = "night_min";
    private const string ColHolidayMin        = "holiday_min";
    private const string ColOffScheduleMin    = "off_schedule_min";
    private const string ColStatus            = "status";

    /// <summary>Columnas del reporte en orden de visualización.</summary>
    private static readonly IReadOnlyList<ReportColumn> Columns =
    [
        new(ColEmployeeId,     "ID Emp.",          40f,  ColumnAlignment.Center),
        new(ColIdCard,         "Cédula",           70f,  ColumnAlignment.Left),
        new(ColFullName,       "Nombre Completo",  130f, ColumnAlignment.Left),
        new(ColWorkDate,       "Fecha",            60f,  ColumnAlignment.Center),
        new(ColFirstPunchIn,   "Entrada",          55f,  ColumnAlignment.Center),
        new(ColLastPunchOut,   "Salida",           55f,  ColumnAlignment.Center),
        new(ColScheduledEntry, "Hora Prog. Ent.",  60f,  ColumnAlignment.Center),
        new(ColScheduledExit,  "Hora Prog. Sal.",  60f,  ColumnAlignment.Center),
        new(ColScheduledMin,   "Min. Prog.",       50f,  ColumnAlignment.Right),
        new(ColTotalWorked,    "Min. Trabajados",  60f,  ColumnAlignment.Right),
        new(ColRegularMin,     "Min. Regular",     55f,  ColumnAlignment.Right),
        new(ColOvertimeMin,    "Min. HE Ord.",     55f,  ColumnAlignment.Right),
        new(ColNightMin,       "Min. Nocturno",    55f,  ColumnAlignment.Right),
        new(ColHolidayMin,     "Min. Feriado",     55f,  ColumnAlignment.Right),
        new(ColOffScheduleMin, "Min. Fuera Hor.",  60f,  ColumnAlignment.Right),
        new(ColStatus,         "Estado",           55f,  ColumnAlignment.Center),
    ];

    public OvertimeReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<OvertimeReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ReportDefinition> BuildAsync(
        ReportFilterDto filter,
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "[OvertimeReportSource] Construyendo reporte de horas extras. Filtro: {@Filter}", filter);

        var data = await _service.GetOvertimeDataAsync(filter, context.RequestAborted);

        var rows = data.Select(d => (IReadOnlyDictionary<string, object?>)
            new Dictionary<string, object?>
            {
                [ColEmployeeId]     = d.EmployeeId,
                [ColIdCard]         = d.IdCard,
                [ColFullName]       = d.FullName,
                [ColWorkDate]       = d.WorkDate.ToString("dd/MM/yyyy"),
                [ColFirstPunchIn]   = d.FirstPunchIn?.ToString("HH:mm") ?? "—",
                [ColLastPunchOut]   = d.LastPunchOut?.ToString("HH:mm") ?? "—",
                [ColScheduledEntry] = d.ScheduledEntryTime?.ToString("HH:mm") ?? "—",
                [ColScheduledExit]  = d.ScheduledExitTime?.ToString("HH:mm") ?? "—",
                [ColScheduledMin]   = d.ScheduledMinutes,
                [ColTotalWorked]    = d.TotalWorkedMinutes,
                [ColRegularMin]     = d.RegularMinutes,
                [ColOvertimeMin]    = d.OvertimeMinutes,
                [ColNightMin]       = d.NightMinutes,
                [ColHolidayMin]     = d.HolidayMinutes,
                [ColOffScheduleMin] = d.OffScheduleMin,
                [ColStatus]         = d.Status,
            })
            .ToList()
            .AsReadOnly();

        var subtitle = BuildSubtitle(filter, data.Count);
        var userName = context.User.Identity?.Name ?? "anonymous";

        _logger.LogInformation(
            "[OvertimeReportSource] Reporte construido. Filas: {Count}", rows.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Horas Extras",
            FilePrefix  = "Reporte_Horas_Extras",
            Subtitle    = subtitle,
            GeneratedBy = userName,
            GeneratedAt = DateTime.UtcNow,
            Columns     = Columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
        };
    }

    private static string BuildSubtitle(ReportFilterDto filter, int totalRows)
    {
        var parts = new List<string>();

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate.Value:dd/MM/yyyy} — {filter.EndDate.Value:dd/MM/yyyy}");
        else if (filter.StartDate.HasValue)
            parts.Add($"Desde: {filter.StartDate.Value:dd/MM/yyyy}");
        else if (filter.EndDate.HasValue)
            parts.Add($"Hasta: {filter.EndDate.Value:dd/MM/yyyy}");

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            parts.Add($"Empleado ID: {filter.EmployeeId.Value}");

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            parts.Add($"Dependencia ID: {filter.DepartmentId.Value}");

        parts.Add($"Total registros: {totalRows}");

        return string.Join(" | ", parts);
    }
}
