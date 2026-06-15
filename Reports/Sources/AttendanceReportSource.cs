using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de registros de asistencia (migración v1→v2).
/// </summary>
public sealed class AttendanceReportSource : IReportSource
{
    private readonly IReportRepository _repository;
    private readonly ILogger<AttendanceReportSource> _logger;

    public ReportType ReportType => ReportType.Attendance;

    private const string ColEmployee   = "employee";
    private const string ColIdCard     = "id_card";
    private const string ColDepartment = "department";
    private const string ColDate       = "date";
    private const string ColCheckIn    = "check_in";
    private const string ColCheckOut   = "check_out";
    private const string ColHours      = "hours";
    private const string ColStatus     = "status";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColEmployee,   "Empleado",         Width: 2.8f),
        new(ColIdCard,     "Cédula",           Width: 1.4f),
        new(ColDepartment, "Dependencia",      Width: 2.0f),
        new(ColDate,       "Fecha",            Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColCheckIn,    "Hora Entrada",     Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColCheckOut,   "Hora Salida",      Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColHours,      "Horas",            Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColStatus,     "Estado",           Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public AttendanceReportSource(IReportRepository repository, ILogger<AttendanceReportSource> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building Attendance report. Start={Start}, End={End}, EmployeeId={Emp}",
            filter.StartDate, filter.EndDate, filter.EmployeeId);

        var data    = await _repository.GetAttendanceReportDataAsync(filter);
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
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape
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
                [ColCheckOut]   = r.CheckOut?.ToString("HH:mm") ?? "-",
                [ColHours]      = r.HoursWorked.HasValue ? r.HoursWorked.Value.ToString("N2") : "-",
                [ColStatus]     = r.Status,
            });
        }
        return rows;
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
