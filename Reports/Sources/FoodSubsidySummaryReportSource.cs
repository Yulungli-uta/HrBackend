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
/// Origen de datos para el reporte consolidado de subsidio de alimentación del
/// personal de Código de Trabajo. Suma los días efectivamente laborados por
/// empleado en el período (<c>HR.tbl_AttendanceCalculations.FoodSubsidy = 1</c>) y
/// los multiplica por el valor diario parametrizado en <c>HR.tbl_Parameters</c>
/// (<c>FOOD_SUBSIDY_DAILY_VALUE</c>).
/// </summary>
/// <remarks>
/// No filtra por régimen laboral por defecto: el flag <c>FoodSubsidy</c> ya solo se
/// activa para Código de Trabajo, así que el resto de empleados queda excluido
/// naturalmente. El filtro de régimen (<see cref="ReportFilterDto.LaborRegimeId"/>),
/// dependencia, empleado y cédula quedan disponibles como filtros opcionales.
/// </remarks>
public sealed class FoodSubsidySummaryReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<FoodSubsidySummaryReportSource> _logger;

    public ReportType ReportType => ReportType.FoodSubsidySummary;

    private const string ColNro        = "nro";
    private const string ColIdCard     = "id_card";
    private const string ColFullName   = "full_name";
    private const string ColDaysWorked = "days_worked";
    private const string ColUnitValue  = "unit_value";
    private const string ColTotalValue = "total_value";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNro,        "Nro",                             Width: 0.6f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,     "Cédula",                          Width: 1.4f),
        new(ColFullName,   "Nombres y Apellidos",              Width: 2.8f),
        new(ColDaysWorked, "Días Efectivamente Laborados",    Width: 1.4f, Alignment: ColumnAlignment.Right),
        new(ColUnitValue,  "Valor",                           Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColTotalValue, "Total",                           Width: 1.0f, Alignment: ColumnAlignment.Right),
    ];

    public FoodSubsidySummaryReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<FoodSubsidySummaryReportSource> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building FoodSubsidySummary report. Start={Start}, End={End}, DepartmentId={DeptId}, EmployeeId={EmpId}",
            filter.StartDate, filter.EndDate, filter.DepartmentId, filter.EmployeeId);

        var data    = await _service.GetFoodSubsidySummaryDataAsync(filter, context.RequestAborted);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("FoodSubsidySummary report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title       = "Subsidio por Alimentación del Personal de Código de Trabajo",
            FilePrefix  = "Reporte_Subsidio_Alimentacion",
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
        IReadOnlyList<FoodSubsidySummaryReportDto> records)
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
                [ColDaysWorked] = r.DaysWorked,
                [ColUnitValue]  = r.UnitValue.ToString("N2", CultureInfo.InvariantCulture),
                [ColTotalValue] = r.TotalValue.ToString("N2", CultureInfo.InvariantCulture),
            });
        }
        return rows;
    }

    private static string BuildSubtitle(ReportFilterDto filter, IReadOnlyList<FoodSubsidySummaryReportDto> records)
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

        parts.Add($"Total empleados: {records.Count}");
        parts.Add($"Total general: {records.Sum(r => r.TotalValue).ToString("N2", CultureInfo.InvariantCulture)}");

        return string.Join(" | ", parts);
    }
}
