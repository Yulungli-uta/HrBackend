using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de solicitudes de contrato.
/// Incluye conteo de personas solicitadas vs contratadas y estado de la solicitud.
/// </summary>
public sealed class ContractRequestsReportSource : IReportSource
{
    private readonly IContractRequestService _contractRequestService;
    private readonly ILogger<ContractRequestsReportSource> _logger;

    public ReportType ReportType => ReportType.ContractRequests;

    private const string ColId         = "request_id";
    private const string ColDepartment = "department";
    private const string ColModality   = "modality";
    private const string ColHours      = "hours";
    private const string ColRequested  = "requested";
    private const string ColHired      = "hired";
    private const string ColPending    = "pending";
    private const string ColStart      = "start_date";
    private const string ColEnd        = "end_date";
    private const string ColStatus     = "status";
    private const string ColObs        = "observation";
    private const string ColCreated    = "created_at";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColId,         "ID",               Width: 0.7f, Alignment: ColumnAlignment.Right),
        new(ColDepartment, "Dependencia",      Width: 2.4f),
        new(ColModality,   "Modalidad",        Width: 1.4f),
        new(ColHours,      "Horas",            Width: 0.8f, Alignment: ColumnAlignment.Right),
        new(ColRequested,  "Solicitados",      Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColHired,      "Contratados",      Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColPending,    "Pendientes",       Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColStart,      "Fecha Inicio",     Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColEnd,        "Fecha Fin",        Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColStatus,     "Estado",           Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColObs,        "Observaciones",    Width: 2.4f),
        new(ColCreated,    "Creado",           Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public ContractRequestsReportSource(IContractRequestService contractRequestService, ILogger<ContractRequestsReportSource> logger)
    {
        _contractRequestService = contractRequestService ?? throw new ArgumentNullException(nameof(contractRequestService));
        _logger                 = logger                 ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building ContractRequests report. Start={Start}, End={End}, Dept={Dept}, Status={Status}",
            filter.StartDate, filter.EndDate, filter.DepartmentId, filter.Status);

        var records = await _contractRequestService.GetForReportAsync(filter, CancellationToken.None);

        _logger.LogInformation("ContractRequests report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Solicitudes de Contrato",
            FilePrefix  = "Reporte_Solicitudes_Contrato",
            Subtitle    = BuildSubtitle(filter, records.Count),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = records.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColId]         = r.RequestId,
                [ColDepartment] = r.DepartmentName,
                [ColModality]   = r.WorkModalityName ?? "—",
                [ColHours]      = r.NumberHour.ToString("N1"),
                [ColRequested]  = r.NumberOfPeopleToHire,
                [ColHired]      = r.TotalPeopleHired,
                [ColPending]    = r.PendingCount,
                [ColStart]      = r.StartDate?.ToString("dd/MM/yyyy") ?? "—",
                [ColEnd]        = r.EndDate?.ToString("dd/MM/yyyy") ?? "—",
                [ColStatus]     = r.StatusName,
                [ColObs]        = r.Observation ?? "—",
                [ColCreated]    = r.CreatedAt.ToString("dd/MM/yyyy"),
            }).ToList(),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape
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
