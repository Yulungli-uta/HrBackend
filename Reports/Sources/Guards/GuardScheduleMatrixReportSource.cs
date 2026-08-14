using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources.Guards;

/// <summary>
/// Origen de datos para el cronograma imprimible de guardias en formato matriz.
/// Filas = guardias, columnas fijas de identificación + columna dinámica por cada fecha.
/// La celda muestra el código de turno (M/T/N) o "L" si es día libre.
/// </summary>
public sealed class GuardScheduleMatrixReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<GuardScheduleMatrixReportSource> _logger;

    public ReportType ReportType => ReportType.GuardScheduleMatrix;

    // Columnas fijas de identificación
    private const string ColGuard    = "guard_name";
    private const string ColIdCard   = "id_card";
    private const string ColGroup    = "group_name";
    private const string ColLocation = "location_name";

    public GuardScheduleMatrixReportSource(AppDbContext db, ILogger<GuardScheduleMatrixReportSource> logger)
    {
        _db     = db     ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var start = filter.StartDate.HasValue ? DateOnly.FromDateTime(filter.StartDate.Value) : DateOnly.FromDateTime(DateTime.Today);
        var end   = filter.EndDate.HasValue   ? DateOnly.FromDateTime(filter.EndDate.Value)   : start.AddDays(13);

        // Limitar a 31 días para evitar columnas excesivas en el PDF
        if ((end.DayNumber - start.DayNumber) > 30)
            end = start.AddDays(30);

        _logger.LogInformation(
            "Building GuardScheduleMatrix report. Start={Start}, End={End}, GroupId={Group}, LocationId={Loc}",
            start, end, filter.GroupId, filter.LocationId);

        var query = _db.GuardShiftPlannings
            .Include(p => p.Employee).ThenInclude(e => e!.People)
            .Include(p => p.Location)
            .Include(p => p.Group)
            .Include(p => p.Schedule)
            .Where(p => p.IsActiveForAssignment
                     && p.WorkDate >= start
                     && p.WorkDate <= end);

        if (filter.GroupId.HasValue)
            query = query.Where(p => p.GroupId == filter.GroupId.Value);

        if (filter.LocationId.HasValue)
            query = query.Where(p => p.LocationId == filter.LocationId.Value
                                  || p.Location!.ParentLocationId == filter.LocationId.Value);

        if (filter.EmployeeId.HasValue)
            query = query.Where(p => p.EmployeeId == filter.EmployeeId.Value);

        var plannings = await query.ToListAsync();

        // Construir lista de fechas en el rango
        var dates = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
            dates.Add(d);

        // Agrupar planificaciones por (empleado, fecha) → código turno
        var shiftByEmpDate = plannings
            .GroupBy(p => (p.EmployeeId, p.WorkDate))
            .ToDictionary(g => g.Key, g => g.First().Schedule?.ScheduleCode ?? "?");

        // Identificación de cada guardia (usamos el primer registro por empleado)
        var empInfo = plannings
            .GroupBy(p => p.EmployeeId)
            .Select(g =>
            {
                var first = g.OrderBy(p => p.WorkDate).First();
                return new
                {
                    g.Key,
                    Name     = first.Employee?.People.GetFullName() ?? string.Empty,
                    IdCard   = first.Employee?.People?.IdCard ?? "",
                    Group    = first.Group?.Name ?? "—",
                    Location = first.Location?.LocationCode != null
                               ? $"[{first.Location.LocationCode}] {first.Location.LocationName}"
                               : (first.Location?.LocationName ?? "—"),
                };
            })
            .OrderBy(e => e.Name)
            .ToList();

        _logger.LogInformation("GuardScheduleMatrix report: {EmpCount} guards × {DateCount} days.", empInfo.Count, dates.Count);

        // Construir columnas dinámicas (fijas + una por fecha)
        var columns = new List<ReportColumn>
        {
            new(ColGuard,    "Guardia",  Width: 2.4f),
            new(ColIdCard,   "Cédula",   Width: 1.3f),
            new(ColGroup,    "Grupo",    Width: 1.4f),
            new(ColLocation, "Ubicación",Width: 1.6f),
        };

        foreach (var d in dates)
        {
            var key = DateKey(d);
            // Encabezado corto: "12-jun" — ancho proporcional a cantidad de fechas
            columns.Add(new ReportColumn(key, d.ToString("dd-MMM"), Width: 0.55f, Alignment: ColumnAlignment.Center));
        }

        // Construir filas
        var rows = empInfo.Select(emp =>
        {
            var row = new Dictionary<string, object?>
            {
                [ColGuard]    = emp.Name,
                [ColIdCard]   = emp.IdCard,
                [ColGroup]    = emp.Group,
                [ColLocation] = emp.Location,
            };

            foreach (var d in dates)
            {
                var key = DateKey(d);
                row[key] = shiftByEmpDate.TryGetValue((emp.Key, d), out var code) ? code : "L";
            }

            return (IReadOnlyDictionary<string, object?>)row;
        }).ToList();

        var subtitle = new List<string>
        {
            $"Período: {start:dd/MM/yyyy} — {end:dd/MM/yyyy}",
            $"Guardias: {empInfo.Count}",
        };
        if (filter.GroupId.HasValue) subtitle.Add($"Grupo ID: {filter.GroupId}");

        return new ReportDefinition
        {
            Title       = "Cronograma de Guardias",
            FilePrefix  = "Cronograma_Guardias",
            Subtitle    = string.Join(" | ", subtitle),
            GeneratedBy = context.User.Identity?.Name ?? "sistema",
            GeneratedAt = DateTime.Now,
            Columns     = columns,
            Rows        = rows,
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
        };
    }

    /// <summary>Clave de columna para una fecha (formato seguro para diccionario).</summary>
    private static string DateKey(DateOnly d) => $"d_{d:yyyyMMdd}";
}
