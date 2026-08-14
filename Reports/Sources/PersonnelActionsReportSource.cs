using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de acciones de personal (todos los estados y categorías).
/// </summary>
public sealed class PersonnelActionsReportSource : IReportSource
{
    private readonly IPersonnelActionService _actionService;
    private readonly ILogger<PersonnelActionsReportSource> _logger;

    public ReportType ReportType => ReportType.PersonnelActions;

    private const string ColNumber     = "number";
    private const string ColIdCard     = "id_card";
    private const string ColPerson     = "person";
    private const string ColDepartment = "department";
    private const string ColType       = "action_type";
    private const string ColCategory   = "category";
    private const string ColDate       = "action_date";
    private const string ColEffective  = "effective_date";
    private const string ColEnd        = "end_date";
    private const string ColStatus     = "status";
    private const string ColDoc        = "document";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNumber,     "N° Acción",         Width: 1.6f),
        new(ColIdCard,     "Cédula",            Width: 1.4f),
        new(ColPerson,     "Persona",           Width: 2.6f),
        new(ColDepartment, "Dependencia",       Width: 2.0f),
        new(ColType,       "Tipo Acción",       Width: 1.8f),
        new(ColCategory,   "Categoría",         Width: 1.4f),
        new(ColDate,       "Fecha Acción",      Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEffective,  "Fecha Efectiva",    Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEnd,        "Fecha Fin",         Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColStatus,     "Estado",            Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColDoc,        "Documento",         Width: 1.0f, Alignment: ColumnAlignment.Center),
    ];

    public PersonnelActionsReportSource(IPersonnelActionService actionService, ILogger<PersonnelActionsReportSource> logger)
    {
        _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
        _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building PersonnelActions report. Start={Start}, End={End}, Status={Status}, Categories={Cats}",
            filter.StartDate, filter.EndDate, filter.Status,
            filter.ActionCategories != null ? string.Join(",", filter.ActionCategories) : "todas");

        var records = await _actionService.GetForReportAsync(filter, CancellationToken.None);

        _logger.LogInformation("PersonnelActions report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Acciones de Personal",
            FilePrefix  = "Reporte_Acciones_Personal",
            Subtitle    = BuildSubtitle(filter, records.Count),
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
                [ColStatus]     = r.StatusName,
                [ColDoc]        = r.HasDocument ? "Sí" : "No",
            }).ToList(),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    private static string MapCategory(string? category) => category switch
    {
        "MOVEMENT"     => "Movimiento",
        "ENTRY"        => "Ingreso",
        "ECONOMIC"     => "Económica",
        "LEAVE"        => "Licencia",
        "DISCIPLINARY" => "Disciplinaria",
        "EXIT"         => "Salida",
        _              => category ?? "—"
    };

    private static string BuildSubtitle(ReportFilterDto filter, int count)
    {
        var parts = new List<string>();
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate:dd/MM/yyyy} — {filter.EndDate:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(filter.Status)) parts.Add($"Estado: {filter.Status}");
        if (filter.ActionCategories?.Length > 0)
            parts.Add($"Categorías: {string.Join(", ", filter.ActionCategories)}");
        parts.Add($"Total: {count}");
        return string.Join(" | ", parts);
    }
}
