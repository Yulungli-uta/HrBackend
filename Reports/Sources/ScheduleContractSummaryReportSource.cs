using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte resumen de empleados por horario asignado,
/// agrupado por tipo de contrato.
/// </summary>
public sealed class ScheduleContractSummaryReportSource : IReportSource
{
    private readonly IvwEmployeeDetailsService _employeeDetailsService;
    private readonly ILogger<ScheduleContractSummaryReportSource> _logger;

    public ReportType ReportType => ReportType.ScheduleContractSummary;

    private const string ColDepartament = "departament";
    private const string ColSchedule = "schedule";
    private const string ColContractType = "contract_type";
    private const string ColTotal = "total_employees";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColDepartament,  "Departamento",    Width: 2.2f, Alignment: ColumnAlignment.Left),
        new(ColSchedule,     "Horario",         Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColContractType, "Tipo Contrato",   Width: 2.0f, Alignment: ColumnAlignment.Center),
        new(ColTotal,        "Total",           Width: 1.0f, Alignment: ColumnAlignment.Center)
    ];

    public ScheduleContractSummaryReportSource(
        IvwEmployeeDetailsService employeeDetailsService,
        ILogger<ScheduleContractSummaryReportSource> logger)
    {
        _employeeDetailsService = employeeDetailsService ?? throw new ArgumentNullException(nameof(employeeDetailsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building ScheduleContractSummary report. DepartmentId={DepartmentId}, EmployeeTypeId={EmployeeTypeId}",
            filter.DepartmentId,
            filter.EmployeeTypeId);

        var summary = (await _employeeDetailsService.GetScheduleContractCountsAsync(
            filter.DepartmentId,
            filter.EmployeeTypeId,
            CancellationToken.None)).ToList();
      
        _logger.LogInformation(
            "ScheduleContractSummary report: {Count} grouped records retrieved.",
            summary.Count);

        return new ReportDefinition
        {
            Title = "Reporte Resumen por Horario y Tipo de Contrato",
            FilePrefix = "Reporte_Resumen_Horario_Contrato",
            Subtitle = BuildSubtitle(filter, summary),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = _columns,
            Rows = BuildRows(summary),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<ScheduleContractCountDto> items)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(items.Count);

        foreach (var item in items)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColDepartament] = string.IsNullOrWhiteSpace(item.DepartmentName) ? "Sin dependencia" : item.DepartmentName,
                [ColSchedule] = item.Schedule,
                [ColContractType] = item.ContractType,
                [ColTotal] = item.TotalEmployees
            });
        }

        return rows;
    }

    private static string BuildSubtitle(
        ReportFilterDto filter,
        IReadOnlyList<ScheduleContractCountDto> items)
    {
        var parts = new List<string>();

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            parts.Add($"DepartmentId: {filter.DepartmentId.Value}");
        else
            parts.Add("Dependencia: Todas");

        if (filter.EmployeeTypeId.HasValue && filter.EmployeeTypeId.Value > 0)
            parts.Add($"Tipo Empleado: {MapEmployeeType(filter.EmployeeTypeId.Value)}");
        else
            parts.Add("Tipo Empleado: Todos");

        parts.Add($"Total grupos: {items.Count}");
        parts.Add($"Total personas: {items.Sum(x => x.TotalEmployees)}");

        return string.Join(" | ", parts);
    }

    private static string MapEmployeeType(int employeeType) => employeeType switch
    {
        1 => "Docente",
        2 => "Administrativo",
        3 => "Trabajador",
        _ => employeeType.ToString()
    };
}