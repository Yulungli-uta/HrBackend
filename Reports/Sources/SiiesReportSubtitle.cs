using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Subtítulo común de los reportes SIIES: deja a la vista el rango de fechas con el que se generó el reporte y, cuando se elige un
/// período académico, el rango de fechas que abarca ese período. El subtítulo sale en el encabezado del PDF y de la vista previa;
/// el CSV de carga a SIIES no lleva encabezado y no cambia.
/// </summary>
internal static class SiiesReportSubtitle
{
    private const string DateFormat = "dd/MM/yyyy";
    private const int MaxCodesListed = 6;

    /// <summary>
    /// Subtítulo de SIIES Funcionarios: el rango pedido o, sin fechas, la fecha de hoy (solo personal vigente hoy).
    /// </summary>
    public static string ForEmployees(ReportFilterDto filter, int totalRows)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var scope = filter.StartDate.HasValue && filter.EndDate.HasValue
            ? RangeText(filter.StartDate.Value, filter.EndDate.Value)
            : $"Personal vigente al {DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture)}";

        return $"{scope} | Total registros: {totalRows}";
    }

    /// <summary>
    /// Subtítulo de SIIES Profesores y Formación Profesional. Con rango de fechas: el rango y los períodos académicos que se cruzan
    /// con él. Con período académico: su código y las fechas que abarca. Sin ninguno: el período más reciente de cada docente.
    /// </summary>
    public static async Task<string> ForTeachersAsync(AppDbContext db, ReportFilterDto filter, int totalRows, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(filter);

        string scope;
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            scope = RangeText(filter.StartDate.Value, filter.EndDate.Value);
            var codes = await SiiesAcademicPeriodHelper.GetOverlappingPeriodCodesAsync(db, filter.StartDate.Value, filter.EndDate.Value, ct);
            if (codes.Count > 0)
            {
                var listed = string.Join(", ", codes.Take(MaxCodesListed));
                scope += codes.Count > MaxCodesListed ? $" (períodos académicos: {listed}…)" : $" (períodos académicos: {listed})";
            }
        }
        else if (!string.IsNullOrWhiteSpace(filter.PeriodCode))
        {
            var code = filter.PeriodCode.Trim();
            var bounds = await db.AcademicHoursDistributions
                .AsNoTracking()
                .Where(a => a.PeriodCode == code && a.PeriodStart != null && a.PeriodEnd != null)
                .GroupBy(a => a.PeriodCode)
                .Select(g => new { Start = g.Min(a => a.PeriodStart), End = g.Max(a => a.PeriodEnd) })
                .FirstOrDefaultAsync(ct);

            scope = bounds?.Start is { } start && bounds.End is { } end
                ? $"Período académico {code}: {start.ToString(DateFormat, CultureInfo.InvariantCulture)} — {end.ToString(DateFormat, CultureInfo.InvariantCulture)}"
                : $"Período académico {code}";
        }
        else
        {
            scope = "Período académico más reciente de cada docente";
        }

        return $"{scope} | Total registros: {totalRows}";
    }

    private static string RangeText(DateTime start, DateTime end) =>
        $"Rango: {start.ToString(DateFormat, CultureInfo.InvariantCulture)} — {end.ToString(DateFormat, CultureInfo.InvariantCulture)}";
}
