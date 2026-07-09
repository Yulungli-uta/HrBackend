using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de dependencias/departamentos.
/// Consulta EF Core directamente (arquitectura v2 genérica) — ya no depende de
/// HR.tbl_Faculties, tabla obsoleta eliminada del esquema real.
/// </summary>
public sealed class DepartmentsReportSource : IReportSource
{
    private readonly IDepartmentsReportService _service;
    private readonly ILogger<DepartmentsReportSource> _logger;

    public ReportType ReportType => ReportType.Departments;

    private const string ColCode       = "code";
    private const string ColName       = "name";
    private const string ColFaculty    = "faculty";
    private const string ColTotal      = "total";
    private const string ColActive     = "active";
    private const string ColInactive   = "inactive";
    private const string ColAvgSalary  = "avg_salary";
    private const string ColStatus     = "status";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColCode,      "Código",            Width: 1.2f),
        new(ColName,      "Dependencia",       Width: 2.8f),
        new(ColFaculty,   "Facultad",          Width: 2.0f),
        new(ColTotal,     "N° Empleados",      Width: 1.4f, Alignment: ColumnAlignment.Right),
        new(ColActive,    "Activos",           Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColInactive,  "Inactivos",         Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColAvgSalary, "RMU Promedio",      Width: 1.4f, Alignment: ColumnAlignment.Right),
        new(ColStatus,    "Estado",            Width: 1.0f, Alignment: ColumnAlignment.Center),
    ];

    public DepartmentsReportSource(IDepartmentsReportService service, ILogger<DepartmentsReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building Departments report. IncludeInactive={IncludeInactive}",
            filter.IncludeInactive);

        var data    = await _service.GetDepartmentsDataAsync(filter, context.RequestAborted);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("Departments report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Dependencias",
            FilePrefix  = "Reporte_Dependencias",
            Subtitle    = $"Total: {records.Count}" + (filter.IncludeInactive == true ? " (incluye inactivos)" : " (solo activos)"),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = BuildRows(records),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<DepartmentReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        foreach (var r in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColCode]      = r.DepartmentCode,
                [ColName]      = r.DepartmentName,
                [ColFaculty]   = string.IsNullOrWhiteSpace(r.FacultyName) ? "-" : r.FacultyName,
                [ColTotal]     = r.TotalEmployees,
                [ColActive]    = r.ActiveEmployees,
                [ColInactive]  = r.InactiveEmployees,
                [ColAvgSalary] = r.AverageSalary.ToString("N2"),
                [ColStatus]    = r.IsActive ? "Activo" : "Inactivo",
            });
        }
        return rows;
    }
}
