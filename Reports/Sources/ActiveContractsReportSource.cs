using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de contratos vigentes a la fecha actual.
/// Aplica filtro fijo Status=VIGENTE y EndDate >= hoy, más filtro de dependencia opcional.
/// </summary>
public sealed class ActiveContractsReportSource : IReportSource
{
    private readonly IContractsService _contractsService;
    private readonly ILogger<ActiveContractsReportSource> _logger;

    public ReportType ReportType => ReportType.ActiveContracts;

    private const string ColCode       = "code";
    private const string ColIdCard     = "id_card";
    private const string ColPerson     = "person";
    private const string ColDepartment = "department";
    private const string ColJob        = "job_title";
    private const string ColType       = "contract_type";
    private const string ColRegime     = "labor_regime";
    private const string ColModality   = "modality";
    private const string ColHours      = "hours";
    private const string ColStart      = "start_date";
    private const string ColEnd        = "end_date";
    private const string ColCreatedBy  = "created_by";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColCode,       "Código",            Width: 1.4f),
        new(ColIdCard,     "Cédula",            Width: 1.4f),
        new(ColPerson,     "Persona",           Width: 2.6f),
        new(ColDepartment, "Dependencia",       Width: 2.0f),
        new(ColJob,        "Cargo",             Width: 2.0f),
        new(ColType,       "Tipo Contrato",     Width: 1.6f),
        new(ColRegime,     "Régimen",           Width: 1.2f),
        new(ColModality,   "Modalidad",         Width: 1.2f),
        new(ColHours,      "Horas",             Width: 0.8f, Alignment: ColumnAlignment.Right),
        new(ColStart,      "Inicio",            Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEnd,        "Fin",               Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColCreatedBy,  "Creado por",        Width: 2.0f),
    ];

    public ActiveContractsReportSource(IContractsService contractsService, ILogger<ActiveContractsReportSource> logger)
    {
        _contractsService = contractsService ?? throw new ArgumentNullException(nameof(contractsService));
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building ActiveContracts report. Dept={Dept}, ContractType={CT}, LaborRegime={LR}, Status={Status}",
            filter.DepartmentId, filter.ContractTypeId, filter.LaborRegimeId, filter.Status ?? "todos");

        // 2026-07-06: el nombre/documentación del reporte prometía un filtro fijo
        // Status=VIGENTE, pero nunca se aplicaba — devolvía contratos vencidos
        // también si el usuario no lo elegía manualmente. Se fuerza aquí, igual
        // que ActivePersonnelActionsReportSource fuerza sus ActionCategories.
        var activeFilter = filter with { Status = "VIGENTE" };

        var records = await _contractsService.GetForReportAsync(activeFilter, CancellationToken.None);

        _logger.LogInformation("ActiveContracts report: {Count} records.", records.Count);

        var parts = new List<string>();
        if (filter.DepartmentId.HasValue)    parts.Add($"Dependencia ID: {filter.DepartmentId}");
        if (filter.ContractTypeId.HasValue)  parts.Add($"Tipo ID: {filter.ContractTypeId}");
        if (filter.LaborRegimeId.HasValue)   parts.Add($"Régimen ID: {filter.LaborRegimeId}");
        parts.Add($"Estado: {activeFilter.Status}");
        parts.Add($"Total: {records.Count}");
        var subtitle = string.Join(" | ", parts);

        return new ReportDefinition
        {
            Title       = "Reporte de Contratos Vigentes",
            FilePrefix  = "Reporte_Contratos_Vigentes",
            Subtitle    = subtitle,
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = records.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColCode]       = r.ContractCode,
                [ColIdCard]     = r.PersonIdCard,
                [ColPerson]     = r.PersonFullName,
                [ColDepartment] = string.IsNullOrWhiteSpace(r.DepartmentName) ? "Sin dependencia" : r.DepartmentName,
                [ColJob]        = r.JobTitle ?? "—",
                [ColType]       = r.ContractTypeName,
                [ColRegime]     = r.LaborRegimeName ?? "—",
                [ColModality]   = r.WorkModalityName ?? "—",
                [ColHours]      = r.ContractedHours.HasValue ? (object)r.ContractedHours.Value.ToString("N1") : "—",
                [ColStart]      = r.StartDate.ToString("dd/MM/yyyy"),
                [ColEnd]        = r.EndDate.ToString("dd/MM/yyyy"),
                [ColCreatedBy]  = r.CreatedByName ?? "—",
            }).ToList(),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }
}
