using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de resumen de asistencia.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — consultar los datos
/// de asistencia desde el repositorio y construir la <see cref="ReportDefinition"/>
/// con las columnas y filas correctas. No sabe nada de PDF ni Excel.
/// </para>
/// <para>
/// Principio OCP: implementa <see cref="IReportSource"/> sin modificar ningún
/// código existente. El generador <c>AttendanceSumaryReportGenerator</c> permanece
/// intacto para garantizar compatibilidad con los endpoints v1.
/// </para>
/// <para>
/// Las columnas reflejan exactamente las definidas en el generador original
/// (<c>AttendanceSumaryReportGenerator</c>) para garantizar consistencia visual.
/// </para>
/// </remarks>
public sealed class AttendanceSummaryReportSource : IReportSource
{
    private readonly IReportRepository _repository;
    private readonly ILogger<AttendanceSummaryReportSource> _logger;

    /// <summary>
    /// Identificador del tipo de reporte que esta fuente puede construir.
    /// </summary>
    public ReportType ReportType => ReportType.AttendanceSummary;

    // ─── Claves de columna ────────────────────────────────────────────────────
    // Se definen como constantes para evitar errores tipográficos entre
    // la definición de columnas y la construcción de filas.
    private const string ColEmployeeId    = "employee_id";
    private const string ColIdCard        = "id_card";
    private const string ColFullName      = "full_name";
    private const string ColEmployeeType  = "employee_type";
    private const string ColContractType  = "contract_type";
    private const string ColWorkDate      = "work_date";
    private const string ColTotalWorked   = "total_worked_min";
    private const string ColRegular       = "regular_min";
    private const string ColOvertime      = "overtime_min";
    private const string ColNight         = "night_min";
    private const string ColHoliday       = "holiday_min";
    private const string ColWorkday       = "workday_min";
    private const string ColLate          = "late_min";
    private const string ColFood          = "food_subsidy";
    private const string ColJustification = "justification_min";

    /// <summary>
    /// Columnas del reporte en el orden exacto del generador original.
    /// </summary>
    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColEmployeeId,   "ID Empleado",      Width: 25f, Alignment: ColumnAlignment.Right),
        new(ColIdCard,       "Cédula",            Width: 35f),
        new(ColFullName,     "Nombre Completo",   Width: 70f),
        new(ColEmployeeType, "Tipo Empleado",     Width: 40f),
        new(ColContractType, "Tipo Contrato",     Width: 45f),
        new(ColWorkDate,     "Fecha Trabajo",     Width: 35f, Alignment: ColumnAlignment.Center),
        new(ColTotalWorked,  "Min. Trabajados",   Width: 35f, Alignment: ColumnAlignment.Right),
        new(ColRegular,      "Min. Regulares",    Width: 35f, Alignment: ColumnAlignment.Right),
        new(ColOvertime,     "Min. Extra",        Width: 30f, Alignment: ColumnAlignment.Right),
        new(ColNight,        "Min. Nocturnos",    Width: 35f, Alignment: ColumnAlignment.Right),
        new(ColHoliday,      "Min. Feriado",      Width: 30f, Alignment: ColumnAlignment.Right),
        new(ColWorkday,      "Min. Jornada",      Width: 30f, Alignment: ColumnAlignment.Right),
        new(ColLate,         "Atrasos (min)",     Width: 30f, Alignment: ColumnAlignment.Right),
        new(ColFood,         "Subsidio Alim.",    Width: 35f, Alignment: ColumnAlignment.Right),
        new(ColJustification,"Min. Justificados", Width: 40f, Alignment: ColumnAlignment.Right)
    ];

    public AttendanceSummaryReportSource(
        IReportRepository repository,
        ILogger<AttendanceSummaryReportSource> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si el repositorio no devuelve datos válidos.
    /// </exception>
    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Building AttendanceSummary report. Filter: StartDate={Start}, EndDate={End}, EmployeeId={EmpId}, EmployeeType={EmpType}",
            filter.StartDate, filter.EndDate, filter.EmployeeId, filter.EmployeeType);

        var data = await _repository.GetAttendanceSumaryReportDataAsync(filter);
        var records = data?.ToList() ?? [];

        _logger.LogInformation("AttendanceSummary report: {Count} records retrieved.", records.Count);

        var rows = BuildRows(records);
        var subtitle = BuildSubtitle(filter);
        var generatedBy = context.User.Identity?.Name ?? "anonymous";

        // Este reporte tiene 15 columnas: se usa Landscape por defecto.
        // Si el frontend envía orientation="portrait", se respeta.
        var orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape;

        return new ReportDefinition
        {
            Title       = "Reporte de Resumen de Asistencia",
            FilePrefix  = "Reporte_Resumen_Asistencia",
            Subtitle    = subtitle,
            GeneratedBy = generatedBy,
            GeneratedAt = DateTime.UtcNow,
            Columns     = _columns,
            Rows        = rows,
            Orientation = orientation
        };
    }

    // ─── Métodos privados ─────────────────────────────────────────────────────

    /// <summary>
    /// Convierte la lista de DTOs en filas de diccionario para el <see cref="ReportDefinition"/>.
    /// </summary>
    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<AttendanceSumaryDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);

        foreach (var att in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColEmployeeId]    = att.EmployeeID,
                [ColIdCard]        = att.IDCard,
                [ColFullName]      = att.NombreCompleto,
                [ColEmployeeType]  = MapEmployeeType(att.EmployeeType),
                [ColContractType]  = string.IsNullOrWhiteSpace(att.ContractType) ? "N/A" : att.ContractType,
                [ColWorkDate]      = att.WorkDate.ToString("dd/MM/yyyy"),
                [ColTotalWorked]   = att.TotalWorkedMinutes,
                [ColRegular]       = att.RegularMinutes,
                [ColOvertime]      = att.OvertimeMinutes,
                [ColNight]         = att.NightMinutes,
                [ColHoliday]       = att.MinFeriado,
                [ColWorkday]       = att.MinTotLaboral,
                [ColLate]          = att.Atrazos,
                [ColFood]          = att.Alimentacion.ToString("N2"),
                [ColJustification] = att.MinJustificacion
            });
        }

        return rows;
    }

    /// <summary>
    /// Construye el subtítulo del reporte a partir de los filtros aplicados.
    /// </summary>
    private static string BuildSubtitle(ReportFilterDto filter)
    {
        var parts = new List<string>();

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            parts.Add($"Período: {filter.StartDate:dd/MM/yyyy} — {filter.EndDate:dd/MM/yyyy}");
        else if (filter.StartDate.HasValue)
            parts.Add($"Desde: {filter.StartDate:dd/MM/yyyy}");
        else if (filter.EndDate.HasValue)
            parts.Add($"Hasta: {filter.EndDate:dd/MM/yyyy}");

        if (!string.IsNullOrWhiteSpace(filter.EmployeeType))
            parts.Add($"Tipo de Empleado: {MapEmployeeType(int.TryParse(filter.EmployeeType, out var t) ? t : 0)}");

        if (filter.EmployeeId.HasValue)
            parts.Add($"Empleado ID: {filter.EmployeeId}");

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Todos los registros";
    }

    /// <summary>
    /// Mapea el código numérico de tipo de empleado a su descripción legible.
    /// Centralizado aquí para mantener consistencia con el generador original.
    /// </summary>
    private static string MapEmployeeType(int employeeType) => employeeType switch
    {
        1 => "Docente",
        2 => "Administrativo",
        3 => "Trabajador",
        _ => employeeType == 0 ? "Todos" : employeeType.ToString()
    };
}
