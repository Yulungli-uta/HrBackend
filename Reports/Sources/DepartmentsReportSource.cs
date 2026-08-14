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

    private const string ColName       = "name";
    private const string ColType       = "department_type";
    private const string ColScope      = "department_scope";
    private const string ColParent     = "parent_department";
    private const string ColTotal      = "total";
    private const string ColStatus     = "status";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColName,      "Dependencia",       Width: 2.8f),
        new(ColType,      "Tipo",              Width: 1.6f),
        new(ColScope,     "Ámbito",            Width: 1.6f),
        new(ColParent,    "Dependencia Padre", Width: 2.2f),
        new(ColTotal,     "N° Empleados",      Width: 1.4f, Alignment: ColumnAlignment.Right),
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
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
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
                [ColName]      = r.DepartmentName,
                [ColType]      = string.IsNullOrWhiteSpace(r.DepartmentTypeName) ? "—" : r.DepartmentTypeName,
                [ColScope]     = string.IsNullOrWhiteSpace(r.DepartmentScopeName) ? "—" : r.DepartmentScopeName,
                [ColParent]    = string.IsNullOrWhiteSpace(r.ParentDepartmentName) ? "—" : r.ParentDepartmentName,
                [ColTotal]     = r.TotalEmployees,
                [ColStatus]    = r.IsActive ? "Activo" : "Inactivo",
            });
        }
        return rows;
    }
}
