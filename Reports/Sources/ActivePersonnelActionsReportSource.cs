using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de acciones vigentes hoy.
/// Aplica filtro fijo Status=FINALIZADO y categorías MOVEMENT, ENTRY, ECONOMIC.
/// </summary>
public sealed class ActivePersonnelActionsReportSource : IReportSource
{
    private readonly IPersonnelActionService _actionService;
    private readonly ILogger<ActivePersonnelActionsReportSource> _logger;

    public ReportType ReportType => ReportType.ActivePersonnelActions;

    private static readonly string[] ActiveCategories = ["MOVEMENT", "ENTRY", "ECONOMIC"];

    private const string ColNumber     = "number";
    private const string ColIdCard     = "id_card";
    private const string ColPerson     = "person";
    private const string ColDepartment = "department";
    private const string ColType       = "action_type";
    private const string ColCategory   = "category";
    private const string ColDate       = "action_date";
    private const string ColEffective  = "effective_date";
    private const string ColEnd        = "end_date";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNumber,     "N° Acción",         Width: 1.6f),
        new(ColIdCard,     "Cédula",            Width: 1.4f),
        new(ColPerson,     "Persona",           Width: 2.8f),
        new(ColDepartment, "Dependencia",       Width: 2.2f),
        new(ColType,       "Tipo Acción",       Width: 1.8f),
        new(ColCategory,   "Categoría",         Width: 1.4f),
        new(ColDate,       "Fecha Acción",      Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEffective,  "Fecha Efectiva",    Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEnd,        "Fecha Fin",         Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public ActivePersonnelActionsReportSource(IPersonnelActionService actionService, ILogger<ActivePersonnelActionsReportSource> logger)
    {
        _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
        _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var activeFilter = filter with { ActionCategories = ActiveCategories };

        _logger.LogInformation("Building ActivePersonnelActions report. Dept={Dept}", filter.DepartmentId);

        var records = await _actionService.GetForReportAsync(activeFilter, CancellationToken.None);

        _logger.LogInformation("ActivePersonnelActions report: {Count} records.", records.Count);

        var subtitle = filter.DepartmentId.HasValue
            ? $"Dependencia ID: {filter.DepartmentId} | Total vigentes: {records.Count}"
            : $"Todas las dependencias | Total vigentes: {records.Count}";

        return new ReportDefinition
        {
            Title       = "Reporte de Acciones de Personal Vigentes",
            FilePrefix  = "Reporte_Acciones_Vigentes",
            Subtitle    = subtitle,
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = records.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColNumber]     = r.ActionNumber ?? $"AP-{r.ActionId}",
                [ColIdCard]     = r.PersonIdCard,
                [ColPerson]     = r.PersonFullName,
                [ColDepartment] = r.DepartmentName ?? "—",
                [ColType]       = r.ActionTypeName,
                [ColCategory]   = MapCategory(r.ActionCategory),
                [ColDate]       = r.ActionDate.ToString("dd/MM/yyyy"),
                [ColEffective]  = r.EffectiveDate?.ToString("dd/MM/yyyy") ?? "—",
                [ColEnd]        = r.EndDate?.ToString("dd/MM/yyyy") ?? "Indefinida",
            }).ToList(),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    private static string MapCategory(string? category) => category switch
    {
        "MOVEMENT" => "Movimiento",
        "ENTRY"    => "Ingreso",
        "ECONOMIC" => "Económica",
        _          => category ?? "—"
    };
}
