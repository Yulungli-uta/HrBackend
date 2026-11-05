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
/// Origen de datos para el reporte consolidado de horas nocturnas por empleado.
/// Suma <c>HR.tbl_AttendanceCalculations.NightMinutes</c> según la ventana nocturna
/// parametrizada en <c>HR.tbl_Parameters</c> (<c>NIGHT_START</c> / <c>NIGHT_END</c>).
/// </summary>
/// <remarks>
/// No filtra por régimen laboral por defecto: a diferencia del subsidio de
/// alimentación, la jornada nocturna no es exclusiva de un tipo de contrato. El
/// filtro de régimen, dependencia, empleado y cédula quedan disponibles como
/// filtros opcionales.
/// </remarks>
public sealed class NightHoursSummaryReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<NightHoursSummaryReportSource> _logger;

    public ReportType ReportType => ReportType.NightHoursSummary;

    private const string ColNro        = "nro";
    private const string ColIdCard     = "id_card";
    private const string ColFullName   = "full_name";
    private const string ColDepartment = "department";
    private const string ColSchedule   = "assigned_schedule";
    private const string ColDays       = "days_with_night_hours";
    private const string ColTotalHours = "total_night_hours";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNro,        "Nro",                       Width: 0.6f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,     "Cédula",                     Width: 1.3f),
        new(ColFullName,   "Nombres y Apellidos",        Width: 2.6f),
        new(ColDepartment, "Dependencia",                Width: 2.0f),
        new(ColSchedule,   "Horario Asignado",           Width: 1.5f, Alignment: ColumnAlignment.Center),
        new(ColDays,      "Jornadas con Horas Nocturnas", Width: 1.3f, Alignment: ColumnAlignment.Right),
        new(ColTotalHours, "Total Horas Nocturnas",      Width: 1.2f, Alignment: ColumnAlignment.Right),
    ];

    public NightHoursSummaryReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<NightHoursSummaryReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building NightHoursSummary report. Start={Start}, End={End}, DepartmentId={DeptId}, EmployeeId={EmpId}",
            filter.StartDate, filter.EndDate, filter.DepartmentId, filter.EmployeeId);

        var result  = await _service.GetNightHoursSummaryDataAsync(filter, context.RequestAborted);
        var records = result.Rows.ToList();

        _logger.LogInformation(
            "NightHoursSummary report: {Count} records. Excluded: {Excluded} people.", records.Count, result.ExcludedPeople);

        return new ReportDefinition
        {
            Title       = "Horas Nocturnas por Empleado",
            FilePrefix  = "Reporte_Horas_Nocturnas",
            Subtitle    = BuildSubtitle(filter, records, result),
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
        IReadOnlyList<NightHoursSummaryReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        var nro = 1;
        foreach (var r in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColNro]        = nro++,
                [ColIdCard]     = r.IdCard,
                [ColFullName]   = r.FullName,
                [ColDepartment] = r.DepartmentName ?? "-",
                [ColSchedule]   = FormatSchedules(r.AssignedSchedules),
                [ColDays]       = r.DaysWithNightHours,
                [ColTotalHours] = r.TotalNightHours.ToString("N2", CultureInfo.InvariantCulture),
            });
        }
        return rows;
    }

    /// <summary>Más de este número de horarios distintos en el período se resume como «Varios».</summary>
    private const int MaxSchedulesListed = 3;

    private static string FormatSchedules(IReadOnlyList<NightHoursScheduleRange> schedules)
    {
        if (schedules.Count == 0) return "-";
        if (schedules.Count > MaxSchedulesListed) return "Varios";
        return string.Join(" / ", schedules.Select(s => $"{s.Entry:HH:mm} - {s.Exit:HH:mm}"));
    }

    private static string BuildSubtitle(
        ReportFilterDto filter, IReadOnlyList<NightHoursSummaryReportDto> records, NightHoursSummaryResultDto result)
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

        // Siempre se declara si se excluyó o no, para que quien lea el archivo sepa qué está viendo y los totales cuadren.
        if (filter.ExcludeGuards == true)
        {
            parts.Add(
                $"Excluidos por cargo ({string.Join(", ", result.ExcludedJobNames)}): " +
                $"{result.ExcludedPeople} personas, {result.ExcludedHours.ToString("N2", CultureInfo.InvariantCulture)} h");
        }
        else if (filter.ExcludeGuards == false)
        {
            parts.Add("Incluye guardias y supervisor");
        }

        parts.Add($"Total empleados: {records.Count}");
        parts.Add($"Total horas nocturnas: {records.Sum(r => r.TotalNightHours).ToString("N2", CultureInfo.InvariantCulture)}");

        return string.Join(" | ", parts);
    }
}
