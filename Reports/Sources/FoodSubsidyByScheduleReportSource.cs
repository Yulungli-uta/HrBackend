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
/// Origen de datos para el reporte de subsidio de alimentación del personal de Código
/// de Trabajo, agrupado por empleado y horario trabajado (sin fecha individual). Suma
/// las jornadas calificadas (<c>HR.tbl_AttendanceCalculations.FoodSubsidy = 1</c>) que
/// cada empleado cumplió en cada horario distinto durante el período.
/// </summary>
/// <remarks>
/// Complementa a <see cref="FoodSubsidySummaryReportSource"/> (un solo total por
/// empleado, sin distinguir horario): este reporte separa por horario dentro de cada
/// empleado — útil para guardias con turno doble, que aparecen con una fila por cada
/// horario distinto que cumplieron.
/// </remarks>
public sealed class FoodSubsidyByScheduleReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<FoodSubsidyByScheduleReportSource> _logger;

    public ReportType ReportType => ReportType.FoodSubsidyBySchedule;

    private const string ColNro        = "nro";
    private const string ColIdCard     = "id_card";
    private const string ColFullName   = "full_name";
    private const string ColTimeRange  = "time_range";
    private const string ColJourneys   = "journeys_count";
    private const string ColUnitValue  = "unit_value";
    private const string ColTotalValue = "total_value";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNro,        "Nro",                  Width: 0.6f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,     "Cédula",               Width: 1.2f),
        new(ColFullName,   "Nombres y Apellidos",  Width: 2.6f),
        new(ColTimeRange,  "Horario",              Width: 1.4f, Alignment: ColumnAlignment.Center),
        new(ColJourneys,   "Jornadas Calificadas", Width: 1.2f, Alignment: ColumnAlignment.Right),
        new(ColUnitValue,  "Valor",                Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColTotalValue, "Total",                Width: 1.0f, Alignment: ColumnAlignment.Right),
    ];

    public FoodSubsidyByScheduleReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<FoodSubsidyByScheduleReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building FoodSubsidyBySchedule report. Start={Start}, End={End}, DepartmentId={DeptId}, EmployeeId={EmpId}",
            filter.StartDate, filter.EndDate, filter.DepartmentId, filter.EmployeeId);

        var data    = await _service.GetFoodSubsidyByScheduleDataAsync(filter, context.RequestAborted);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("FoodSubsidyBySchedule report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Subsidio por Alimentación del Personal de Código de Trabajo — por Empleado y Horario",
            FilePrefix  = "Reporte_Subsidio_Alimentacion_Horario",
            Subtitle    = BuildSubtitle(filter, records),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = _columns,
            Rows        = BuildRows(records),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Portrait,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<FoodSubsidyByScheduleReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        var nro = 1;
        foreach (var r in records)
        {
            var timeRange = r.EntryTime.HasValue && r.ExitTime.HasValue
                ? $"{r.EntryTime.Value:HH:mm} - {r.ExitTime.Value:HH:mm}"
                : "-";

            rows.Add(new Dictionary<string, object?>
            {
                [ColNro]        = nro++,
                [ColIdCard]     = r.IdCard,
                [ColFullName]   = r.FullName,
                [ColTimeRange]  = timeRange,
                [ColJourneys]   = r.JourneysCount,
                [ColUnitValue]  = r.UnitValue.ToString("N2", CultureInfo.InvariantCulture),
                [ColTotalValue] = r.TotalValue.ToString("N2", CultureInfo.InvariantCulture),
            });
        }
        return rows;
    }

    private static string BuildSubtitle(ReportFilterDto filter, IReadOnlyList<FoodSubsidyByScheduleReportDto> records)
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

        parts.Add($"Total filas: {records.Count}");
        parts.Add($"Total jornadas: {records.Sum(r => r.JourneysCount)}");
        parts.Add($"Total general: {records.Sum(r => r.TotalValue).ToString("N2", CultureInfo.InvariantCulture)}");

        return string.Join(" | ", parts);
    }
}
