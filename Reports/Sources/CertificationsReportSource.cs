using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de certificaciones financieras.
/// Incluye solicitud de contrato asociada, dependencia, estado y motivo de rechazo si aplica.
/// </summary>
public sealed class CertificationsReportSource : IReportSource
{
    private readonly IFinancialCertificationService _certificationService;
    private readonly ILogger<CertificationsReportSource> _logger;

    public ReportType ReportType => ReportType.Certifications;

    private const string ColId         = "cert_id";
    private const string ColCode       = "cert_code";
    private const string ColNumber     = "cert_number";
    private const string ColBudget     = "budget";
    private const string ColRmuHour    = "rmu_hour";
    private const string ColRmuCon     = "rmu_con";
    private const string ColBudgetDate = "budget_date";
    private const string ColDepartment = "department";
    private const string ColRequestId  = "request_id";
    private const string ColPeople     = "people_requested";
    private const string ColStatus     = "status";
    private const string ColRejection  = "rejection_reason";
    private const string ColCreated    = "created_at";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColId,         "ID",               Width: 0.7f, Alignment: ColumnAlignment.Right),
        new(ColCode,       "Código",           Width: 1.6f),
        new(ColNumber,     "N° Certificación", Width: 1.6f),
        new(ColBudget,     "Presupuesto",      Width: 1.6f),
        new(ColRmuHour,    "RMU/Hora",         Width: 1.2f, Alignment: ColumnAlignment.Right),
        new(ColRmuCon,     "RMU/Contrato",     Width: 1.2f, Alignment: ColumnAlignment.Right),
        new(ColBudgetDate, "Fecha Pres.",      Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColDepartment, "Dependencia",      Width: 2.0f),
        new(ColRequestId,  "Solicitud",        Width: 0.9f, Alignment: ColumnAlignment.Right),
        new(ColPeople,     "N° Personas",      Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColStatus,     "Estado",           Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColRejection,  "Motivo Rechazo",   Width: 2.0f),
        new(ColCreated,    "Creado",           Width: 1.2f, Alignment: ColumnAlignment.Center),
    ];

    public CertificationsReportSource(IFinancialCertificationService certificationService, ILogger<CertificationsReportSource> logger)
    {
        _certificationService = certificationService ?? throw new ArgumentNullException(nameof(certificationService));
        _logger               = logger               ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building Certifications report. Start={Start}, End={End}, Status={Status}",
            filter.StartDate, filter.EndDate, filter.Status);

        var records = await _certificationService.GetForReportAsync(filter, CancellationToken.None);

        _logger.LogInformation("Certifications report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Certificaciones Financieras",
            FilePrefix  = "Reporte_Certificaciones",
            Subtitle    = BuildSubtitle(filter, records.Count),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = records.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                [ColId]         = r.CertificationId,
                [ColCode]       = r.CertCode,
                [ColNumber]     = r.CertNumber ?? "—",
                [ColBudget]     = r.Budget ?? "—",
                [ColRmuHour]    = r.RmuHour.HasValue ? (object)r.RmuHour.Value.ToString("N2") : "—",
                [ColRmuCon]     = r.RmuCon.HasValue  ? (object)r.RmuCon.Value.ToString("N2")  : "—",
                [ColBudgetDate] = r.CertBudgetDate?.ToString("dd/MM/yyyy") ?? "—",
                [ColDepartment] = r.DepartmentName,
                [ColRequestId]  = r.RequestId.HasValue ? (object)r.RequestId.Value : "—",
                [ColPeople]     = r.NumberOfPeopleRequested.HasValue ? (object)r.NumberOfPeopleRequested.Value : "—",
                [ColStatus]     = r.StatusName,
                [ColRejection]  = r.RejectionReason ?? "—",
                [ColCreated]    = r.CreatedAt?.ToString("dd/MM/yyyy") ?? "—",
            }).ToList(),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape
        };
    }

    private static string BuildSubtitle(ReportFilterDto filter, int count)
    {
        var parts = new List<string>();
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate:dd/MM/yyyy} — {filter.EndDate:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(filter.Status)) parts.Add($"Estado: {filter.Status}");
        else parts.Add("Todos los estados");
        parts.Add($"Total: {count}");
        return string.Join(" | ", parts);
    }
}
