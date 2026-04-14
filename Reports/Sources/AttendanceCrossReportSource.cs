using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte cruzado de asistencia.
/// Consolida en una sola vista: horas trabajadas, permisos, vacaciones,
/// justificaciones y licencias médicas por empleado/día.
/// </summary>
/// <remarks>
/// <para>
/// Este reporte es el más completo del sistema de asistencia: permite al área de RRHH
/// ver en una sola vista el comportamiento de asistencia de cada empleado,
/// cruzando tiempo trabajado con todas las novedades del día.
/// </para>
/// <para>
/// Dado el alto número de columnas, la orientación por defecto es <c>Landscape</c>.
/// </para>
/// </remarks>
public sealed class AttendanceCrossReportSource : IReportSource
{
    private readonly IAttendanceCalculationsReportService _service;
    private readonly ILogger<AttendanceCrossReportSource> _logger;

    /// <inheritdoc/>
    public ReportType ReportType => ReportType.AttendanceCross;

    // ─── Claves de columna ────────────────────────────────────────────────────
    private const string ColEmployeeId          = "employee_id";
    private const string ColIdCard              = "id_card";
    private const string ColFullName            = "full_name";
    private const string ColWorkDate            = "work_date";
    private const string ColScheduledMin        = "scheduled_min";
    private const string ColTotalWorked         = "total_worked_min";
    private const string ColRegularMin          = "regular_min";
    private const string ColOvertimeMin         = "overtime_min";
    private const string ColAbsentMin           = "absent_min";
    private const string ColTardinessMin        = "tardiness_min";
    private const string ColEarlyLeave          = "early_leave_min";
    private const string ColHasPermission       = "has_permission";
    private const string ColPermissionMin       = "permission_min";
    private const string ColHasVacation         = "has_vacation";
    private const string ColVacationMin         = "vacation_min";
    private const string ColHasJustification    = "has_justification";
    private const string ColJustificationMin    = "justification_min";
    private const string ColHasMedicalLeave     = "has_medical_leave";
    private const string ColMedicalLeaveMin     = "medical_leave_min";
    private const string ColPaidLeaveMin        = "paid_leave_min";
    private const string ColUnpaidLeaveMin      = "unpaid_leave_min";
    private const string ColFoodSubsidy         = "food_subsidy";
    private const string ColStatus              = "status";

    /// <summary>Columnas del reporte en orden de visualización.</summary>
    private static readonly IReadOnlyList<ReportColumn> Columns =
    [
        new(ColEmployeeId,       "ID Emp.",          40f,  ColumnAlignment.Center),
        new(ColIdCard,           "Cédula",           70f,  ColumnAlignment.Left),
        new(ColFullName,         "Nombre Completo",  120f, ColumnAlignment.Left),
        new(ColWorkDate,         "Fecha",            55f,  ColumnAlignment.Center),
        new(ColScheduledMin,     "Min. Prog.",       50f,  ColumnAlignment.Right),
        new(ColTotalWorked,      "Min. Trabaj.",     55f,  ColumnAlignment.Right),
        new(ColRegularMin,       "Min. Regular",     55f,  ColumnAlignment.Right),
        new(ColOvertimeMin,      "Min. HE",          45f,  ColumnAlignment.Right),
        new(ColAbsentMin,        "Min. Ausente",     55f,  ColumnAlignment.Right),
        new(ColTardinessMin,     "Min. Tardanza",    55f,  ColumnAlignment.Right),
        new(ColEarlyLeave,       "Min. Sal. Ant.",   55f,  ColumnAlignment.Right),
        new(ColHasPermission,    "Permiso",          45f,  ColumnAlignment.Center),
        new(ColPermissionMin,    "Min. Permiso",     55f,  ColumnAlignment.Right),
        new(ColHasVacation,      "Vacación",         45f,  ColumnAlignment.Center),
        new(ColVacationMin,      "Min. Vacac.",      50f,  ColumnAlignment.Right),
        new(ColHasJustification, "Justif.",          40f,  ColumnAlignment.Center),
        new(ColJustificationMin, "Min. Justif.",     55f,  ColumnAlignment.Right),
        new(ColHasMedicalLeave,  "Lic. Méd.",        45f,  ColumnAlignment.Center),
        new(ColMedicalLeaveMin,  "Min. Lic. Méd.",   55f,  ColumnAlignment.Right),
        new(ColPaidLeaveMin,     "Min. Lic. Con G.", 60f,  ColumnAlignment.Right),
        new(ColUnpaidLeaveMin,   "Min. Lic. Sin G.", 60f,  ColumnAlignment.Right),
        new(ColFoodSubsidy,      "Subsidio Alim.",   55f,  ColumnAlignment.Right),
        new(ColStatus,           "Estado",           55f,  ColumnAlignment.Center),
    ];

    public AttendanceCrossReportSource(
        IAttendanceCalculationsReportService service,
        ILogger<AttendanceCrossReportSource> logger)
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
            "[AttendanceCrossReportSource] Construyendo reporte cruzado. Filtro: {@Filter}", filter);

        var data = await _service.GetAttendanceCrossDataAsync(filter, context.RequestAborted);

        var rows = data.Select(d => (IReadOnlyDictionary<string, object?>)
            new Dictionary<string, object?>
            {
                [ColEmployeeId]       = d.EmployeeId,
                [ColIdCard]           = d.IdCard,
                [ColFullName]         = d.FullName,
                [ColWorkDate]         = d.WorkDate.ToString("dd/MM/yyyy"),
                [ColScheduledMin]     = d.ScheduledMinutes,
                [ColTotalWorked]      = d.TotalWorkedMinutes,
                [ColRegularMin]       = d.RegularMinutes,
                [ColOvertimeMin]      = d.OvertimeMinutes,
                [ColAbsentMin]        = d.AbsentMinutes,
                [ColTardinessMin]     = d.TardinessMin,
                [ColEarlyLeave]       = d.EarlyLeaveMinutes,
                [ColHasPermission]    = d.HasPermission    ? "Sí" : "No",
                [ColPermissionMin]    = d.PermissionMinutes,
                [ColHasVacation]      = d.HasVacation      ? "Sí" : "No",
                [ColVacationMin]      = d.VacationMinutes,
                [ColHasJustification] = d.HasJustification ? "Sí" : "No",
                [ColJustificationMin] = d.JustificationMinutes,
                [ColHasMedicalLeave]  = d.HasMedicalLeave  ? "Sí" : "No",
                [ColMedicalLeaveMin]  = d.MedicalLeaveMinutes,
                [ColPaidLeaveMin]     = d.PaidLeaveMinutes,
                [ColUnpaidLeaveMin]   = d.UnpaidLeaveMinutes,
                [ColFoodSubsidy]      = d.FoodSubsidy,
                [ColStatus]           = d.Status,
            })
            .ToList()
            .AsReadOnly();

        var subtitle = BuildSubtitle(filter, data.Count);
        var userName = context.User.Identity?.Name ?? "anonymous";

        _logger.LogInformation(
            "[AttendanceCrossReportSource] Reporte construido. Filas: {Count}", rows.Count);

        return new ReportDefinition
        {
            Title       = "Reporte Cruzado de Asistencia",
            FilePrefix  = "Reporte_Cruzado_Asistencia",
            Subtitle    = subtitle,
            GeneratedBy = userName,
            GeneratedAt = DateTime.UtcNow,
            Columns     = Columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
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
