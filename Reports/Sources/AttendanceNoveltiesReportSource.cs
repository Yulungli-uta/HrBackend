using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de novedades de asistencia, para todo el personal.
/// Una fila por (jornada, tipo de novedad): ausencia injustificada, picada sin captura
/// confiable, atraso, salida anticipada, ajuste manual, horas fuera de horario,
/// recuperación aplicada y reemplazo de guardia. Incluye una observación en texto
/// estándar con los valores concretos de esa jornada ya interpolados.
/// </summary>
public sealed class AttendanceNoveltiesReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<AttendanceNoveltiesReportSource> _logger;

    public ReportType ReportType => ReportType.AttendanceNovelties;

    private const string ColNro         = "nro";
    private const string ColIdCard      = "id_card";
    private const string ColFullName    = "full_name";
    private const string ColWorkDate    = "work_date";
    private const string ColJourney     = "journey_number";
    private const string ColSchedule    = "schedule";
    private const string ColNovelty     = "novelty_label";
    private const string ColObservation = "observation";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNro,         "Nro",                 Width: 0.5f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,      "Cédula",              Width: 1.1f),
        new(ColFullName,    "Nombres y Apellidos", Width: 2.2f),
        new(ColWorkDate,    "Fecha",               Width: 0.9f, Alignment: ColumnAlignment.Center),
        new(ColJourney,     "Jornada",             Width: 0.6f, Alignment: ColumnAlignment.Center),
        new(ColSchedule,    "Horario",             Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColNovelty,     "Tipo de Novedad",     Width: 1.6f),
        new(ColObservation, "Observación",         Width: 3.0f),
    ];

    public AttendanceNoveltiesReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<AttendanceNoveltiesReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building AttendanceNovelties report. Start={Start}, End={End}, DepartmentId={DeptId}, EmployeeId={EmpId}",
            filter.StartDate, filter.EndDate, filter.DepartmentId, filter.EmployeeId);

        var data    = await _service.GetAttendanceNoveltiesDataAsync(filter, context.RequestAborted);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("AttendanceNovelties report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Novedades de Asistencia",
            FilePrefix  = "Reporte_Novedades_Asistencia",
            Subtitle    = BuildSubtitle(filter, records),
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
        IReadOnlyList<AttendanceNoveltyReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        var nro = 1;
        foreach (var r in records)
        {
            var schedule = r.ScheduledEntryTime.HasValue && r.ScheduledExitTime.HasValue
                ? $"{r.ScheduledEntryTime.Value:HH:mm} - {r.ScheduledExitTime.Value:HH:mm}"
                : "-";

            rows.Add(new Dictionary<string, object?>
            {
                [ColNro]         = nro++,
                [ColIdCard]      = r.IdCard,
                [ColFullName]    = r.FullName,
                [ColWorkDate]    = r.WorkDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                [ColJourney]     = r.JourneyNumber,
                [ColSchedule]    = schedule,
                [ColNovelty]     = r.NoveltyLabel,
                [ColObservation] = r.Observation,
            });
        }
        return rows;
    }

    private static string BuildSubtitle(ReportFilterDto filter, IReadOnlyList<AttendanceNoveltyReportDto> records)
    {
        var parts = new List<string>();

        if (filter.StartDate.HasValue)
        {
            var mes = filter.StartDate.Value.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("es-EC"));
            parts.Add($"Mes: {mes.ToUpperInvariant()}");
        }
        else if (filter.EndDate.HasValue)
        {
            parts.Add($"Hasta: {filter.EndDate:dd/MM/yyyy}");
        }

        parts.Add($"Total novedades: {records.Count}");
        parts.Add($"Empleados distintos: {records.Select(r => r.EmployeeId).Distinct().Count()}");

        return string.Join(" | ", parts);
    }
}
