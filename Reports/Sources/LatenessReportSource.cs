using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de atrasos.
/// Consulta <c>HR.tbl_AttendanceCalculations</c> a través de
/// <see cref="IAttendanceCalculationsReportService"/> y construye la
/// <see cref="ReportDefinition"/> con las columnas y filas del reporte.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — obtener los datos
/// de atrasos y construir la definición del reporte. No sabe nada de PDF ni Excel.
/// </para>
/// <para>
/// Principio OCP: implementa <see cref="IReportSource"/> sin modificar ningún
/// código existente. El pipeline de renderizado (<see cref="Services.ReportServiceV2"/>)
/// la descubre automáticamente por inyección de dependencias.
/// </para>
/// </remarks>
public sealed class LatenessReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<LatenessReportSource> _logger;

    /// <inheritdoc/>
    public ReportType ReportType => ReportType.Lateness;

    // ─── Claves de columna ────────────────────────────────────────────────────
    private const string ColEmployeeId         = "employee_id";
    private const string ColIdCard             = "id_card";
    private const string ColFullName           = "full_name";
    private const string ColWorkDate           = "work_date";
    private const string ColScheduledEntry     = "scheduled_entry";
    private const string ColFirstPunchIn       = "first_punch_in";
    private const string ColMinutesLate        = "minutes_late";
    private const string ColTardinessMin       = "tardiness_min";
    private const string ColEarlyLeave         = "early_leave_min";
    private const string ColHasJustification   = "has_justification";
    private const string ColJustificationMin   = "justification_min";
    private const string ColStatus             = "status";

    /// <summary>Columnas del reporte en orden de visualización.</summary>
    private static readonly IReadOnlyList<ReportColumn> Columns =
    [
        new(ColEmployeeId,       "ID Emp.",          40f,  ColumnAlignment.Center),
        new(ColIdCard,           "Cédula",           70f,  ColumnAlignment.Left),
        new(ColFullName,         "Nombre Completo",  130f, ColumnAlignment.Left),
        new(ColWorkDate,         "Fecha",            60f,  ColumnAlignment.Center),
        new(ColScheduledEntry,   "Hora Prog.",       55f,  ColumnAlignment.Center),
        new(ColFirstPunchIn,     "Entrada Real",     65f,  ColumnAlignment.Center),
        new(ColMinutesLate,      "Min. Atraso",      55f,  ColumnAlignment.Right),
        new(ColTardinessMin,     "Min. Tardanza",    60f,  ColumnAlignment.Right),
        new(ColEarlyLeave,       "Min. Sal. Ant.",   60f,  ColumnAlignment.Right),
        new(ColHasJustification, "Justificado",      55f,  ColumnAlignment.Center),
        new(ColJustificationMin, "Min. Justif.",     55f,  ColumnAlignment.Right),
        new(ColStatus,           "Estado",           60f,  ColumnAlignment.Center),
    ];

    public LatenessReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<LatenessReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ReportDefinition> BuildAsync(
        ReportFilterDto filter,
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "[LatenessReportSource] Construyendo reporte de atrasos. Filtro: {@Filter}", filter);

        var data = await _service.GetLatenessDataAsync(filter, context.RequestAborted);

        var rows = data.Select(d => (IReadOnlyDictionary<string, object?>)
            new Dictionary<string, object?>
            {
                [ColEmployeeId]       = d.EmployeeId,
                [ColIdCard]           = d.IdCard,
                [ColFullName]         = d.FullName,
                [ColWorkDate]         = d.WorkDate.ToString("dd/MM/yyyy"),
                [ColScheduledEntry]   = d.ScheduledEntryTime?.ToString("HH:mm") ?? "—",
                [ColFirstPunchIn]     = d.FirstPunchIn?.ToString("HH:mm") ?? "—",
                [ColMinutesLate]      = d.MinutesLate,
                [ColTardinessMin]     = d.TardinessMin,
                [ColEarlyLeave]       = d.EarlyLeaveMinutes,
                [ColHasJustification] = d.HasJustification ? "Sí" : "No",
                [ColJustificationMin] = d.JustificationMinutes,
                [ColStatus]           = d.Status,
            })
            .ToList()
            .AsReadOnly();

        var subtitle = BuildSubtitle(filter, data.Count);
        var userName = context.User.Identity?.Name ?? "anonymous";

        _logger.LogInformation(
            "[LatenessReportSource] Reporte construido. Filas: {Count}", rows.Count);

        return new ReportDefinition
        {
            Title       = "Reporte de Atrasos",
            FilePrefix  = "Reporte_Atrasos",
            Subtitle    = subtitle,
            GeneratedBy = userName,
            GeneratedAt = DateTime.UtcNow,
            Columns     = Columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Portrait,
        };
    }

    private static string BuildSubtitle(ReportFilterDto filter, int totalRows)
    {
        var parts = new List<string>();

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate.Value:dd/MM/yyyy} — {filter.EndDate.Value:dd/MM/yyyy}");
        else if (filter.StartDate.HasValue)
            parts.Add($"Desde: {filter.StartDate.Value:dd/MM/yyyy}");
        else if (filter.EndDate.HasValue)
            parts.Add($"Hasta: {filter.EndDate.Value:dd/MM/yyyy}");

        if (filter.EmployeeId.HasValue && filter.EmployeeId.Value > 0)
            parts.Add($"Empleado ID: {filter.EmployeeId.Value}");

        if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            parts.Add($"Dependencia ID: {filter.DepartmentId.Value}");

        parts.Add($"Total registros: {totalRows}");

        return string.Join(" | ", parts);
    }
}
