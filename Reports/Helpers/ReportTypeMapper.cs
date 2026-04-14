using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Helpers;

/// <summary>
/// Convierte el slug de URL recibido en el endpoint genérico al enum <see cref="ReportType"/>
/// de forma centralizada y segura.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — mapear strings a
/// <see cref="ReportType"/>. Los endpoints no deben contener esta lógica de mapeo.
/// </para>
/// <para>
/// Los slugs están diseñados para coincidir exactamente con los valores que el
/// frontend envía en la URL: <c>/api/v1/rh/reports/{reportType}/preview</c>.
/// </para>
/// <para>
/// Slugs registrados:
/// <list type="table">
///   <listheader><term>Slug</term><description>ReportType</description></listheader>
///   <item><term>employees</term><description><see cref="ReportType.Employees"/></description></item>
///   <item><term>attendance</term><description><see cref="ReportType.Attendance"/></description></item>
///   <item><term>departments</term><description><see cref="ReportType.Departments"/></description></item>
///   <item><term>attendancesumary</term><description><see cref="ReportType.AttendanceSummary"/></description></item>
/// </list>
/// </para>
/// </remarks>
public static class ReportTypeMapper
{
    /// <summary>
    /// Mapa inmutable de slug → <see cref="ReportType"/>.
    /// Usar <c>StringComparer.OrdinalIgnoreCase</c> para tolerar variaciones de mayúsculas
    /// sin comprometer la seguridad del mapeo.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, ReportType> _slugMap =
        new Dictionary<string, ReportType>(StringComparer.OrdinalIgnoreCase)
        {
            ["employees"]       = ReportType.Employees,
            ["attendance"]      = ReportType.Attendance,
            ["departments"]     = ReportType.Departments,
            // El frontend envía "attendancesumary" (un solo 'm') — se mantiene por compatibilidad
            ["attendancesumary"]  = ReportType.AttendanceSummary,
            ["attendancesummary"] = ReportType.AttendanceSummary,

            ["employees-by-department"] = ReportType.EmployeesByDepartment,
            ["department-contract-summary"] = ReportType.DepartmentContractSummary,
            ["schedule-contract-summary"] = ReportType.ScheduleContractSummary,

            // ── Reportes v2 — AttendanceCalculations ─────────────────────────
            ["lateness"]          = ReportType.Lateness,
            ["overtime"]          = ReportType.Overtime,
            ["attendance-cross"]  = ReportType.AttendanceCross,
        };

    /// <summary>
    /// Intenta convertir el slug de URL al <see cref="ReportType"/> correspondiente.
    /// </summary>
    /// <param name="slug">Slug recibido en la URL (p.ej. <c>"employees"</c>).</param>
    /// <param name="reportType">
    /// Cuando el método devuelve <c>true</c>, contiene el <see cref="ReportType"/> mapeado.
    /// </param>
    /// <returns>
    /// <c>true</c> si el slug es válido y tiene un mapeo registrado; <c>false</c> en caso contrario.
    /// </returns>
    public static bool TryParse(string slug, out ReportType reportType)
    {
        reportType = default;
        if (string.IsNullOrWhiteSpace(slug)) return false;
        return _slugMap.TryGetValue(slug, out reportType);
    }

    /// <summary>
    /// Convierte el slug al <see cref="ReportType"/> o lanza una excepción si no es válido.
    /// </summary>
    /// <param name="slug">Slug recibido en la URL.</param>
    /// <returns>El <see cref="ReportType"/> correspondiente.</returns>
    /// <exception cref="ArgumentException">
    /// Se lanza si el slug no tiene un mapeo registrado.
    /// </exception>
    public static ReportType Parse(string slug)
    {
        if (TryParse(slug, out var reportType)) return reportType;
        throw new ArgumentException(
            $"Tipo de reporte no reconocido: '{slug}'. " +
            $"Valores válidos: {string.Join(", ", _slugMap.Keys)}",
            nameof(slug));
    }

    /// <summary>
    /// Devuelve todos los slugs registrados.
    /// Útil para documentación o validación en swagger.
    /// </summary>
    public static IEnumerable<string> GetRegisteredSlugs() => _slugMap.Keys;
}
