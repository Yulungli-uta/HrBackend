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
/// Origen de datos para el resumen mensual del reporte "Horas Extra: Planificado vs.
/// Ejecutado". Misma fuente que <see cref="OvertimePlanDailyReportSource"/>, agregada por
/// (empleado, mes calendario) — suma todos los planes que el empleado tuvo ese mes en una
/// sola fila. No filtra por fecha (muestra todo el historial) ni expone estado del plan,
/// a diferencia del detalle diario.
/// </summary>
public sealed class OvertimePlanMonthlyReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<OvertimePlanMonthlyReportSource> _logger;

    public ReportType ReportType => ReportType.OvertimePlanMonthly;

    private const string ColIdCard         = "id_card";
    private const string ColFullName       = "full_name";
    private const string ColDepartment     = "department";
    private const string ColPeriod         = "period";
    private const string ColPlanCount      = "plan_count";
    private const string ColAssignedHours  = "assigned_hours";
    private const string ColExecutedHours  = "executed_hours";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColIdCard,         "Cédula",            1.2f),
        new(ColFullName,       "Nombres y Apellidos", 2.6f),
        new(ColDepartment,     "Dependencia",       1.8f),
        new(ColPeriod,         "Mes",               0.9f, ColumnAlignment.Center),
        new(ColPlanCount,      "N° Planes",         0.8f, ColumnAlignment.Center),
        new(ColAssignedHours,  "Horas Planificadas (Total)", 1.2f, ColumnAlignment.Right),
        new(ColExecutedHours,  "Horas Ejecutadas (Mes)",     1.2f, ColumnAlignment.Right),
    ];

    public OvertimePlanMonthlyReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<OvertimePlanMonthlyReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var data = await _service.GetOvertimePlanMonthlyDataAsync(filter, context.RequestAborted);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("OvertimePlanMonthly report: {Count} filas.", records.Count);

        return new ReportDefinition
        {
            Title       = "Horas Extra: Planificado vs. Ejecutado (Resumen Mensual)",
            FilePrefix  = "Reporte_HorasExtra_Planificado_Mensual",
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
        IReadOnlyList<OvertimePlanMonthlyReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        foreach (var r in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColIdCard]        = r.IdCard,
                [ColFullName]      = r.FullName,
                [ColDepartment]    = r.DepartmentName ?? "-",
                [ColPeriod]        = r.Period,
                [ColPlanCount]     = r.PlanCount,
                [ColAssignedHours] = r.AssignedHours.ToString("N2", CultureInfo.InvariantCulture),
                [ColExecutedHours] = Math.Round(r.ExecutedTotalMinutes / 60m, 2).ToString("N2", CultureInfo.InvariantCulture),
            });
        }
        return rows;
    }

    private static string BuildSubtitle(ReportFilterDto filter, IReadOnlyList<OvertimePlanMonthlyReportDto> records)
    {
        var parts = new List<string>();

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            parts.Add($"Empleado ID: {filter.EmployeeId.Value}");

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            parts.Add($"Dependencia ID: {filter.DepartmentId.Value}");

        parts.Add($"Total registros: {records.Count}");

        return string.Join(" | ", parts);
    }
}
