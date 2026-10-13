using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el detalle diario del reporte "Horas Extra: Planificado vs.
/// Ejecutado". Una fila por día ejecutado (<c>HR.tbl_TimePlanningExecution</c>), con la
/// cuota planificada del plan (<c>HR.tbl_TimePlanningEmployees.AssignedHours</c>) y el
/// estado de aprobación/pago de esa fecha en <c>HR.tbl_Overtime</c>, cuando existe.
/// </summary>
public sealed class OvertimePlanDailyReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<OvertimePlanDailyReportSource> _logger;

    public ReportType ReportType => ReportType.OvertimePlanDaily;

    private const string ColIdCard         = "id_card";
    private const string ColFullName       = "full_name";
    private const string ColDepartment     = "department";
    private const string ColPlanTitle      = "plan_title";
    private const string ColWorkDate       = "work_date";
    private const string ColAssignedHours  = "assigned_hours";
    private const string ColExecutedHours  = "executed_hours";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColIdCard,         "Cédula",            1.2f),
        new(ColFullName,       "Nombres y Apellidos", 2.4f),
        new(ColDepartment,     "Dependencia",       1.8f),
        new(ColPlanTitle,      "Plan",              1.8f),
        new(ColWorkDate,       "Fecha",             1.0f, ColumnAlignment.Center),
        new(ColAssignedHours,  "Horas Planificadas", 1.0f, ColumnAlignment.Right),
        new(ColExecutedHours,  "Horas Ejecutadas (día)", 1.0f, ColumnAlignment.Right),
    ];

    public OvertimePlanDailyReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<OvertimePlanDailyReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var data = await _service.GetOvertimePlanDailyDataAsync(filter, context.RequestAborted);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("OvertimePlanDaily report: {Count} filas.", records.Count);

        return new ReportDefinition
        {
            Title       = "Horas Extra: Planificado vs. Ejecutado (Detalle Diario)",
            FilePrefix  = "Reporte_HorasExtra_Planificado_Diario",
            Subtitle    = BuildSubtitle(filter, records),
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
        IReadOnlyList<OvertimePlanDailyReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        foreach (var r in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColIdCard]         = r.IdCard,
                [ColFullName]       = r.FullName,
                [ColDepartment]     = r.DepartmentName ?? "-",
                [ColPlanTitle]      = r.PlanTitle,
                [ColWorkDate]       = r.WorkDate.ToString("dd/MM/yyyy"),
                [ColAssignedHours]  = (r.AssignedHours ?? 0).ToString("N2", CultureInfo.InvariantCulture),
                [ColExecutedHours]  = Math.Round(r.ExecutedTotalMinutes / 60m, 2).ToString("N2", CultureInfo.InvariantCulture),
            });
        }
        return rows;
    }

    private static string BuildSubtitle(ReportFilterDto filter, IReadOnlyList<OvertimePlanDailyReportDto> records)
    {
        var parts = new List<string>();

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate.Value:dd/MM/yyyy} — {filter.EndDate.Value:dd/MM/yyyy}");

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            parts.Add($"Empleado ID: {filter.EmployeeId.Value}");

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            parts.Add($"Dependencia ID: {filter.DepartmentId.Value}");

        parts.Add($"Total registros: {records.Count}");

        return string.Join(" | ", parts);
    }
}
