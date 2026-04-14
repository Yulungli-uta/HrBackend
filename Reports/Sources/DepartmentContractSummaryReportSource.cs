using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte resumen de empleados por dependencia,
/// agrupado por tipo de contrato.
/// </summary>
public sealed class DepartmentContractSummaryReportSource : IReportSource
{
    private readonly IvwEmployeeDetailsService _employeeDetailsService;
    private readonly ILogger<DepartmentContractSummaryReportSource> _logger;

    public ReportType ReportType => ReportType.DepartmentContractSummary;

    private const string ColDepartment = "department";
    private const string ColContractType = "contract_type";
    private const string ColTotal = "total_employees";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColDepartment,   "Dependencia",    Width: 2.5f),
        new(ColContractType, "Tipo Contrato",  Width: 2.0f),
        new(ColTotal,        "Total",          Width: 1.0f, Alignment: ColumnAlignment.Right)
    ];

    public DepartmentContractSummaryReportSource(
        IvwEmployeeDetailsService employeeDetailsService,
        ILogger<DepartmentContractSummaryReportSource> logger)
    {
        _employeeDetailsService = employeeDetailsService ?? throw new ArgumentNullException(nameof(employeeDetailsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building DepartmentContractSummary report. DepartmentId={DepartmentId}, EmployeeTypeId={EmployeeTypeId}",
            filter.DepartmentId,
            filter.EmployeeTypeId);

        var summary = (await _employeeDetailsService.GetDepartmentContractCountsAsync(
            filter.DepartmentId,
            filter.EmployeeTypeId,
            CancellationToken.None)).ToList();

        _logger.LogInformation(
            "DepartmentContractSummary report: {Count} grouped records retrieved.",
            summary.Count);

        return new ReportDefinition
        {
            Title = "Reporte Resumen por Dependencia y Tipo de Contrato",
            FilePrefix = "Reporte_Resumen_Dependencia_Contrato",
            Subtitle = BuildSubtitle(filter, summary),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = _columns,
            Rows = BuildRows(summary),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<DepartmentContractCountDto> items)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(items.Count);

        foreach (var item in items)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColDepartment] = item.Department,
                [ColContractType] = item.ContractType,
                [ColTotal] = item.TotalEmployees
            });
        }

        return rows;
    }

    private static string BuildSubtitle(
        ReportFilterDto filter,
        IReadOnlyList<DepartmentContractCountDto> items)
    {
        var parts = new List<string>();

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
        {
            var departmentName = items
                .Select(x => x.Department)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            parts.Add($"Dependencia: {departmentName ?? $"ID {filter.DepartmentId.Value}"}");
        }
        else
        {
            parts.Add("Dependencia: Todas");
        }

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