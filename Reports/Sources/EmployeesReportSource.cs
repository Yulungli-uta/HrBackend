using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte general de empleados.
/// Datos resueltos vía EF Core (IEmployeesService) — arquitectura v2, sin stored procedures.
/// </summary>
public sealed class EmployeesReportSource : IReportSource
{
    private readonly IEmployeesService _employeesService;
    private readonly ILogger<EmployeesReportSource> _logger;

    public ReportType ReportType => ReportType.Employees;

    private const string ColId         = "id";
    private const string ColIdCard     = "id_card";
    private const string ColFullName   = "full_name";
    private const string ColDepartment = "department";
    private const string ColEmpType    = "employee_type";
    private const string ColJobTitle   = "job_title";
    private const string ColContract   = "contract_type";
    private const string ColStartDate  = "start_date";
    private const string ColSalary     = "salary";
    private const string ColStatus     = "status";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColId,         "ID",               Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColIdCard,     "Cédula",           Width: 1.4f),
        new(ColFullName,   "Nombre Completo",  Width: 3.0f),
        new(ColDepartment, "Dependencia",      Width: 2.0f),
        new(ColEmpType,    "Régimen",          Width: 1.6f),
        new(ColJobTitle,   "Cargo",            Width: 2.0f),
        new(ColContract,   "Tipo Contrato",    Width: 1.6f),
        new(ColStartDate,  "Fecha Ingreso",    Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColSalary,     "RMU",              Width: 1.2f, Alignment: ColumnAlignment.Right),
        new(ColStatus,     "Estado",           Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public EmployeesReportSource(IEmployeesService employeesService, ILogger<EmployeesReportSource> logger)
    {
        _employeesService = employeesService ?? throw new ArgumentNullException(nameof(employeesService));
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building Employees report. DepartmentId={Dept}, IsActive={Active}",
            filter.DepartmentId, filter.IsActive);

        var data = await _employeesService.GetEmployeesReportDataAsync(
            filter.DepartmentId,
            filter.EmployeeTypeId,
            filter.IsActive,
            filter.StartDate,
            filter.EndDate);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("Employees report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Empleados",
            FilePrefix  = "Reporte_Empleados",
            Subtitle    = BuildSubtitle(filter, records.Count),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = BuildRows(records),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<EmployeeReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        foreach (var r in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColId]         = r.Id,
                [ColIdCard]     = r.IdentificationNumber,
                [ColFullName]   = r.FullName,
                [ColDepartment] = string.IsNullOrWhiteSpace(r.DepartmentName) ? "Sin dependencia" : r.DepartmentName,
                [ColEmpType]    = r.EmployeeType,
                [ColJobTitle]   = string.IsNullOrWhiteSpace(r.JobTitle) ? "Sin cargo" : r.JobTitle,
                [ColContract]   = r.ContractType ?? "Sin contrato",
                [ColStartDate]  = r.HireDate.ToString("dd/MM/yyyy"),
                [ColSalary]     = r.BaseSalary.ToString("N2"),
                [ColStatus]     = r.IsActive ? "Activo" : "Inactivo",
            });
        }
        return rows;
    }

    private static string BuildSubtitle(ReportFilterDto filter, int count)
    {
        var parts = new List<string>();
        if (filter.DepartmentId.HasValue) parts.Add($"Dependencia ID: {filter.DepartmentId}");
        if (filter.IsActive.HasValue)     parts.Add(filter.IsActive.Value ? "Solo activos" : "Solo inactivos");
        parts.Add($"Total: {count}");
        return string.Join(" | ", parts);
    }
}
