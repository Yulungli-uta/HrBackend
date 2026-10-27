using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de registros de asistencia.
/// Consulta <c>HR.tbl_AttendanceCalculations</c> vía EF Core
/// (<see cref="IAttendanceCalculationsReportService"/>), arquitectura v2 genérica.
/// </summary>
public sealed class AttendanceReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<AttendanceReportSource> _logger;

    public ReportType ReportType => ReportType.Attendance;

    private const string ColEmployee   = "employee";
    private const string ColIdCard     = "id_card";
    private const string ColDepartment = "department";
    private const string ColDate       = "date";
    private const string ColCheckIn    = "check_in";
    private const string ColLunchOut   = "lunch_out";
    private const string ColLunchIn    = "lunch_in";
    private const string ColCheckOut   = "check_out";
    private const string ColHours      = "hours";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColEmployee,   "Empleado",         Width: 2.8f),
        new(ColIdCard,     "Cédula",           Width: 1.4f),
        new(ColDepartment, "Dependencia",      Width: 2.0f),
        new(ColDate,       "Fecha",            Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColCheckIn,    "Entrada",           Width: 1.3f, Alignment: ColumnAlignment.Center),
        new(ColLunchOut,   "Salida almuerzo",   Width: 1.3f, Alignment: ColumnAlignment.Center),
        new(ColLunchIn,    "Retorno almuerzo",  Width: 1.3f, Alignment: ColumnAlignment.Center),
        new(ColCheckOut,   "Salida laboral",    Width: 1.3f, Alignment: ColumnAlignment.Center),
        new(ColHours,      "Horas (HH:mm)",     Width: 1.1f, Alignment: ColumnAlignment.Right),
    ];

    public AttendanceReportSource(IAttendanceCalculationsReportService service, ILogger<AttendanceReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building Attendance report. Start={Start}, End={End}, EmployeeId={Emp}",
            filter.StartDate, filter.EndDate, filter.EmployeeId);

        var data    = await _service.GetAttendanceDataAsync(filter, context.RequestAborted);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("Attendance report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Asistencia",
            FilePrefix  = "Reporte_Asistencia",
            Subtitle    = BuildSubtitle(filter, records.Count),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = BuildRows(records),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<AttendanceReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        foreach (var r in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColEmployee]   = r.EmployeeName,
                [ColIdCard]     = r.IdentificationNumber,
                [ColDepartment] = string.IsNullOrWhiteSpace(r.DepartmentName) ? "Sin dependencia" : r.DepartmentName,
                [ColDate]       = r.AttendanceDate.ToString("dd/MM/yyyy"),
                [ColCheckIn]    = r.CheckIn?.ToString("HH:mm") ?? "-",
                [ColLunchOut]   = r.LunchOut?.ToString("HH:mm") ?? "-",
                [ColLunchIn]    = r.LunchIn?.ToString("HH:mm") ?? "-",
                [ColCheckOut]   = r.CheckOut?.ToString("HH:mm") ?? "-",
                [ColHours]      = FormatHours(r.HoursWorked),
            });
        }
        return rows;
    }

    /// <summary>Horas decimales (7.5) a HH:mm (07:30); evita leer "9.40" como 9 h 40 min.</summary>
    private static string FormatHours(decimal? hours)
    {
        if (!hours.HasValue) return "-";
        var totalMinutes = (int)Math.Round(hours.Value * 60m, MidpointRounding.AwayFromZero);
        return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }

    private static string BuildSubtitle(ReportFilterDto filter, int count)
    {
        var parts = new List<string>();
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate:dd/MM/yyyy} — {filter.EndDate:dd/MM/yyyy}");
        if (filter.EmployeeId.HasValue) parts.Add($"Empleado ID: {filter.EmployeeId}");
        if (filter.DepartmentId.HasValue) parts.Add($"Dependencia ID: {filter.DepartmentId}");
        parts.Add($"Total: {count}");
        return string.Join(" | ", parts);
    }
}
