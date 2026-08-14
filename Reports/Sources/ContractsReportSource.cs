using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de contratos. Extrae todos los contratos en el
/// rango de fechas indicado, con filtro opcional por dependencia y estado.
/// </summary>
public sealed class ContractsReportSource : IReportSource
{
    private readonly IContractsService _contractsService;
    private readonly ILogger<ContractsReportSource> _logger;

    public ReportType ReportType => ReportType.Contracts;

    private const string ColCode       = "code";
    private const string ColIdCard     = "id_card";
    private const string ColPerson     = "person";
    private const string ColDepartment = "department";
    private const string ColType       = "contract_type";
    private const string ColRegime     = "labor_regime";
    private const string ColModality   = "modality";
    private const string ColHours      = "hours";
    private const string ColStart      = "start_date";
    private const string ColEnd        = "end_date";
    private const string ColStatus     = "status";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColCode,       "Código",            Width: 1.6f),
        new(ColIdCard,     "Cédula",            Width: 1.4f),
        new(ColPerson,     "Persona",           Width: 2.8f),
        new(ColDepartment, "Dependencia",       Width: 2.2f),
        new(ColType,       "Tipo Contrato",     Width: 1.6f),
        new(ColRegime,     "Régimen",           Width: 1.4f),
        new(ColModality,   "Modalidad",         Width: 1.4f),
        new(ColHours,      "Horas",             Width: 0.8f, Alignment: ColumnAlignment.Right),
        new(ColStart,      "Inicio",            Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEnd,        "Fin",               Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColStatus,     "Estado",            Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public ContractsReportSource(IContractsService contractsService, ILogger<ContractsReportSource> logger)
    {
        _contractsService = contractsService ?? throw new ArgumentNullException(nameof(contractsService));
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building Contracts report. Start={Start}, End={End}, Dept={Dept}, Status={Status}",
            filter.StartDate, filter.EndDate, filter.DepartmentId, filter.Status);

        var records = await _contractsService.GetForReportAsync(filter, CancellationToken.None);

        _logger.LogInformation("Contracts report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Contratos",
            FilePrefix  = "Reporte_Contratos",
            Subtitle    = BuildSubtitle(filter, records.Count),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = records.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColCode]       = r.ContractCode,
                [ColIdCard]     = r.PersonIdCard,
                [ColPerson]     = r.PersonFullName,
                [ColDepartment] = string.IsNullOrWhiteSpace(r.DepartmentName) ? "Sin dependencia" : r.DepartmentName,
                [ColType]       = r.ContractTypeName,
                [ColRegime]     = r.LaborRegimeName ?? "—",
                [ColModality]   = r.WorkModalityName ?? "—",
                [ColHours]      = r.ContractedHours.HasValue ? (object)r.ContractedHours.Value.ToString("N1") : "—",
                [ColStart]      = r.StartDate.ToString("dd/MM/yyyy"),
                [ColEnd]        = r.EndDate.ToString("dd/MM/yyyy"),
                [ColStatus]     = r.StatusName,
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
