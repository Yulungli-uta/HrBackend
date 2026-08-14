using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte histórico por empleado:
/// contratos + acciones de categoría MOVEMENT, ENTRY y ECONOMIC (excluye DISCIPLINARY, LEAVE, EXIT).
/// </summary>
public sealed class EmployeeHistoryReportSource : IReportSource
{
    private readonly IContractsService _contractsService;
    private readonly IPersonnelActionService _actionService;
    private readonly ILogger<EmployeeHistoryReportSource> _logger;

    public ReportType ReportType => ReportType.EmployeeHistory;

    private static readonly string[] HistoryCategories = ["MOVEMENT", "ENTRY", "ECONOMIC"];

    private const string ColRecord   = "record_type";
    private const string ColIdCard   = "id_card";
    private const string ColPerson   = "person";
    private const string ColDept     = "department";
    private const string ColDetail1  = "detail_1";
    private const string ColDetail2  = "detail_2";
    private const string ColDateFrom = "date_from";
    private const string ColDateTo   = "date_to";
    private const string ColStatus   = "status";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColRecord,   "Tipo",            Width: 1.2f),
        new(ColIdCard,   "Cédula",          Width: 1.4f),
        new(ColPerson,   "Persona",         Width: 2.8f),
        new(ColDept,     "Dependencia",     Width: 2.2f),
        new(ColDetail1,  "N° Documento",    Width: 1.8f),
        new(ColDetail2,  "Régimen/Cat.",    Width: 1.4f),
        new(ColDateFrom, "Fecha Inicio",    Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColDateTo,   "Fecha Fin",       Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColStatus,   "Estado",          Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public EmployeeHistoryReportSource(
        IContractsService contractsService,
        IPersonnelActionService actionService,
        ILogger<EmployeeHistoryReportSource> logger)
    {
        _contractsService = contractsService ?? throw new ArgumentNullException(nameof(contractsService));
        _actionService    = actionService    ?? throw new ArgumentNullException(nameof(actionService));
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building EmployeeHistory report. EmployeeId={Emp}, DeptId={Dept}",
            filter.EmployeeId, filter.DepartmentId);

        var actionFilter = filter with { ActionCategories = HistoryCategories };

        // Ejecución secuencial: los dos servicios comparten el mismo DbContext (scoped)
        // y EF Core no permite dos operaciones concurrentes sobre la misma instancia.
        var contracts = await _contractsService.GetForReportAsync(filter, CancellationToken.None);
        var actions   = await _actionService.GetForReportAsync(actionFilter, CancellationToken.None);

        _logger.LogInformation(
            "EmployeeHistory report: {Contracts} contratos, {Actions} acciones.",
            contracts.Count, actions.Count);

        var rows = BuildRows(contracts, actions);

        var subtitle = filter.EmployeeId.HasValue
            ? $"Empleado ID: {filter.EmployeeId} | Contratos: {contracts.Count} | Acciones: {actions.Count}"
            : $"Todos | Contratos: {contracts.Count} | Acciones: {actions.Count}";

        return new ReportDefinition
        {
            Title       = "Historial de Empleado (Contratos y Acciones)",
            FilePrefix  = "Historial_Empleado",
            Subtitle    = subtitle,
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<Application.DTOs.Reports.ContractReportDto> contracts,
        IReadOnlyList<Application.DTOs.Reports.PersonnelActionReportDto> actions)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var c in contracts)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColRecord]  = "Contrato",
                [ColIdCard]  = c.PersonIdCard,
                [ColPerson]  = c.PersonFullName,
                [ColDept]    = c.DepartmentName,
                [ColDetail1] = c.ContractTypeName,
                [ColDetail2] = c.LaborRegimeName ?? "—",
                [ColDateFrom]= c.StartDate.ToString("dd/MM/yyyy"),
                [ColDateTo]  = c.EndDate.ToString("dd/MM/yyyy"),
                [ColStatus]  = c.StatusName,
            });
        }

        foreach (var a in actions)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColRecord]  = "Acción",
                [ColIdCard]  = a.PersonIdCard,
                [ColPerson]  = a.PersonFullName,
                [ColDept]    = a.DepartmentName ?? "—",
                [ColDetail1] = a.ActionNumber ?? $"AP-{a.ActionId}",
                [ColDetail2] = MapCategory(a.ActionCategory),
                [ColDateFrom]= a.ActionDate.ToString("dd/MM/yyyy"),
                [ColDateTo]  = a.EndDate?.ToString("dd/MM/yyyy") ?? "Indefinida",
                [ColStatus]  = a.StatusName,
            });
        }

        return rows
            .OrderBy(r => r[ColPerson]?.ToString())
            .ThenBy(r => r[ColDateFrom]?.ToString())
            .ToList();
    }

    private static string MapCategory(string? category) => category switch
    {
        "MOVEMENT" => "Movimiento",
        "ENTRY"    => "Ingreso",
        "ECONOMIC" => "Económica",
        _          => category ?? "—"
    };
}
