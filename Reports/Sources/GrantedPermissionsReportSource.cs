using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de permisos otorgados.
/// Extrae permisos con filtros de rango de fechas, estado y empleado.
/// </summary>
public sealed class GrantedPermissionsReportSource : IReportSource
{
    private readonly IPermissionsService _permissionsService;
    private readonly ILogger<GrantedPermissionsReportSource> _logger;

    public ReportType ReportType => ReportType.GrantedPermissions;

    private const string ColIdCard      = "id_card";
    private const string ColPerson      = "person";
    private const string ColDepartment  = "department";
    private const string ColType        = "permission_type";
    private const string ColStart       = "start_date";
    private const string ColEnd         = "end_date";
    private const string ColHours       = "hours";
    private const string ColVacation    = "charged_vacation";
    private const string ColJustif      = "justification";
    private const string ColStatus      = "status";
    private const string ColApprovedBy  = "approved_by";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColIdCard,     "Cédula",           Width: 1.4f),
        new(ColPerson,     "Empleado",         Width: 2.6f),
        new(ColDepartment, "Dependencia",      Width: 2.0f),
        new(ColType,       "Tipo Permiso",     Width: 1.8f),
        new(ColStart,      "Fecha Inicio",     Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEnd,        "Fecha Fin",        Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColHours,      "Horas",            Width: 0.8f, Alignment: ColumnAlignment.Right),
        new(ColVacation,   "Cargo Vac.",       Width: 0.9f, Alignment: ColumnAlignment.Center),
        new(ColJustif,     "Justificación",    Width: 2.4f),
        new(ColStatus,     "Estado",           Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColApprovedBy, "Aprobado por",     Width: 1.8f),
    ];

    public GrantedPermissionsReportSource(IPermissionsService permissionsService, ILogger<GrantedPermissionsReportSource> logger)
    {
        _permissionsService = permissionsService ?? throw new ArgumentNullException(nameof(permissionsService));
        _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building GrantedPermissions report. Start={Start}, End={End}, Dept={Dept}, Status={Status}",
            filter.StartDate, filter.EndDate, filter.DepartmentId, filter.Status);

        var records = await _permissionsService.GetForReportAsync(filter, CancellationToken.None);

        _logger.LogInformation("GrantedPermissions report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Permisos Otorgados",
            FilePrefix  = "Reporte_Permisos_Otorgados",
            Subtitle    = BuildSubtitle(filter, records.Count),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = records.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColIdCard]     = r.PersonIdCard,
                [ColPerson]     = r.PersonFullName,
                [ColDepartment] = string.IsNullOrWhiteSpace(r.DepartmentName) ? "Sin dependencia" : r.DepartmentName,
                [ColType]       = r.PermissionTypeName,
                [ColStart]      = r.StartDate.ToString("dd/MM/yyyy"),
                [ColEnd]        = r.EndDate.ToString("dd/MM/yyyy"),
                [ColHours]      = r.HourTaken.HasValue ? (object)r.HourTaken.Value.ToString("N2") : "—",
                [ColVacation]   = r.ChargedToVacation ? "Sí" : "No",
                [ColJustif]     = r.Justification ?? "—",
                [ColStatus]     = r.Status,
                [ColApprovedBy] = r.ApprovedByName ?? "—",
            }).ToList(),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    private static string BuildSubtitle(ReportFilterDto filter, int count)
    {
        var parts = new List<string>();
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate:dd/MM/yyyy} — {filter.EndDate:dd/MM/yyyy}");
        if (filter.DepartmentId.HasValue) parts.Add($"Dependencia ID: {filter.DepartmentId}");
        if (!string.IsNullOrWhiteSpace(filter.Status)) parts.Add($"Estado: {filter.Status}");
        parts.Add($"Total: {count}");
        return string.Join(" | ", parts);
    }
}
